using System.Net;
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", async (HttpContext ctx) =>
{
    var symbol = ctx.Request.Query["symbol"].FirstOrDefault() ?? "";
    string resultHtml = string.Empty;

    if (!string.IsNullOrWhiteSpace(symbol))
    {
        var quote = await FetchQuote(symbol);
        resultHtml = quote switch
        {
            QuoteSuccess q => $"<div class=\"card\"><div class=\"title\">{q.Symbol.ToUpperInvariant()}</div><div class=\"price\">{q.Price}</div><div class=\"source\">Source: stooq.com</div></div>",
            QuoteError e => $"<div class=\"error\">{e.Message}</div>",
            _ => "<div class=\"error\">Unknown response</div>"
        };
    }

    return Results.Content($@"<!DOCTYPE html>
<html>
<head>
  <style>
    body {{ font-family: Arial, sans-serif; background: #0d1117; color: #e6edf3; display:flex; align-items:center; justify-content:center; height:100vh; margin:0; }}
    .panel {{ background:#161b22; padding:24px; border-radius:12px; width:420px; box-shadow:0 8px 24px rgba(0,0,0,0.4); }}
    h1 {{ margin-top:0; font-size:22px; }}
    form {{ display:flex; gap:8px; margin:16px 0; }}
    input[type=text] {{ flex:1; padding:10px; border-radius:8px; border:1px solid #30363d; background:#0d1117; color:#e6edf3; }}
    button {{ padding:10px 14px; border:0; border-radius:8px; background:#2ea043; color:#0d1117; font-weight:700; cursor:pointer; }}
    button:hover {{ background:#3fb950; }}
    .card {{ background:#0d1117; padding:16px; border:1px solid #30363d; border-radius:10px; }}
    .title {{ font-size:18px; font-weight:700; }}
    .price {{ font-size:32px; margin-top:8px; }}
    .source {{ margin-top:4px; color:#8b949e; font-size:12px; }}
    .error {{ color:#ff6b6b; margin-top:8px; }}
    .hint {{ color:#8b949e; font-size:12px; }}
  </style>
</head>
<body>
  <div class=""panel"">
    <h1>Quote lookup</h1>
    <form method=""get"">
      <input name=""symbol"" placeholder=""e.g. MSFT, AAPL, TSLA"" value=""{WebUtility.HtmlEncode(symbol)}"" aria-label=""symbol input"" />
      <button type=""submit"">Fetch</button>
    </form>
    <div class=""hint"">Uses stooq.com free quote API; add .us/.pl suffix if needed (e.g. msft.us).</div>
    {resultHtml}
  </div>
</body>
</html>", "text/html");
});

app.MapGet("/api/quote", async (string symbol) =>
{
    var quote = await FetchQuote(symbol);
    return quote switch
    {
        QuoteSuccess q => Results.Ok(q),
        QuoteError e => Results.BadRequest(new { error = e.Message }),
        _ => Results.BadRequest(new { error = "Unknown response" })
    };
});

app.MapGet("/healthz", () => "ok");

app.Run();

static async Task<QuoteResult> FetchQuote(string symbol)
{
    if (string.IsNullOrWhiteSpace(symbol))
    {
        return new QuoteError("Symbol is required");
    }

    // stooq allows free queries without auth; suffix .us for US equities.
    var normalized = symbol.Trim().ToLowerInvariant();
    if (!normalized.Contains('.')) normalized += ".us";

    var url = $"https://stooq.com/q/l/?s={normalized}&f=sd2t2ohlcv&h&e=json";
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

    try
    {
        var response = await client.GetFromJsonAsync<StooqResponse>(url);
        var q = response?.Symbols?.FirstOrDefault();
        if (q == null || q.Close <= 0) return new QuoteError("Quote not found");
        return new QuoteSuccess(q.Symbol, q.Close);
    }
    catch (Exception ex)
    {
        return new QuoteError($"Lookup failed: {ex.Message}");
    }
}

record QuoteResult;
record QuoteSuccess(string Symbol, decimal Price) : QuoteResult;
record QuoteError(string Message) : QuoteResult;

record StooqResponse(StooqQuote[] Symbols);
record StooqQuote(string Symbol, decimal Close);
