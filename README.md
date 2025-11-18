# Sample Argo CD Apps

Two simple Kubernetes apps for Argo CD on AKS:

- `nginx-hello`: NGINX serving a custom index page.
- `echo-server`: HTTP echo server using HashiCorp http-echo.

After pushing this repo, create Argo CD `Application` resources from `applications/`. Update `repoURL` to match your repo URL.
