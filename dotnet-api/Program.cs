var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { message = "Hello from .NET 8 on AKS via Argo CD" }));
app.MapGet("/healthz", () => Results.Ok("ok"));
app.MapGet("/version", () => Results.Ok(new { version = "v1" }));

app.Run();
