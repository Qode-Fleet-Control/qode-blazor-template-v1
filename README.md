# Blazor template

Provisioned from [`Qode-Fleet-Control/fleet-template-v1`](https://github.com/Qode-Fleet-Control/fleet-template-v1) — the fleet
lifecycle contract (`bin/`, `fleet.conf`, `compose.yaml`, deploy workflows) with the stock
Blazor Web App (.NET 10, interactive server rendering) laid on top.

    src/BlazorApp/      the Blazor Web App (Components/, wwwroot/, Program.cs)
    Dockerfile          SDK build stage -> aspnet:10.0 runtime, non-root
    compose.yaml        the fleet's docker runtime (service `app`)
    fleet.conf          the app manifest every bin/ script reads

Pages: `/` (Home), `/counter` (interactive, over the Blazor circuit's WebSocket),
`/weather` (streaming rendering). `GET /health` is the fleet's `HEALTH_PATH`.

## Origin

Generated 2026-10-05 with the official template, inside the official SDK image (.NET SDK 10.0.401):

    docker run --rm -u $(id -u):$(id -g) -e HOME=/tmp -v "$PWD":/w -w /w \
      mcr.microsoft.com/dotnet/sdk:10.0 \
      dotnet new blazor -n BlazorApp -o src/BlazorApp --framework net10.0

## Running it

**On the fleet** — nothing to do: the fleet clones the repo, injects `PORT` / `DATABASE_URL`, and
runs `bin/run`, which (docker runtime) does `docker compose build` then
`docker compose up --remove-orphans` in the foreground. The app listens on `0.0.0.0:$PORT` and
is served at the root of its own hostname (`https://<hash>.<FLEET_APP_DOMAIN>/`); every URL
the template emits is root-relative (`<base href="/">`).

**With docker**

    PORT=8080 bin/run                 # or: docker compose up --build
    open http://localhost:8080/

**Without docker** (needs the .NET 10 SDK on PATH)

    FLEET_RUNTIME=process PORT=8080 bin/run
    # = dotnet restore src/BlazorApp/BlazorApp.csproj
    #   dotnet publish src/BlazorApp/BlazorApp.csproj -c Release --no-restore -o .out
    #   env PORT=8080 dotnet .out/BlazorApp.dll

or, for development with hot reload: `dotnet watch --project src/BlazorApp`.

| step | process runtime | docker runtime |
|---|---|---|
| install | `dotnet restore src/BlazorApp/BlazorApp.csproj` | — |
| build | `dotnet publish … -o .out` | `docker compose build` |
| start | `env PORT="$PORT" dotnet .out/BlazorApp.dll` | `docker compose up --remove-orphans` |

## Deviations from the stock generator output, and why

- **Project named `BlazorApp` under `src/BlazorApp/`.** Not the repo root: .NET writes build
  output to the project's `bin/`/`obj/`, which would collide with the fleet's `bin/` scripts.
  Not `App`: the template's root component is also `App`, and a project (namespace) named
  `App` makes `_Imports.razor` fail to compile (CS0138 — `App` is a type, not a namespace).
- **`Program.cs` binds `http://0.0.0.0:$PORT` when `PORT` is set**, read at runtime — ASP.NET
  Core does not read `PORT` on its own. Without it, Kestrel keeps its usual defaults.
- **`AddHealthChecks()` + `MapHealthChecks("/health")`** for the fleet's health probe.
- **`UseHttpsRedirection()` only in Development.** On the fleet the edge terminates TLS and the
  container speaks plain HTTP. `UseHsts()` stays: browsers see HTTPS at the edge.
- **Dockerfile clears `ASPNETCORE_HTTP_PORTS`** (the aspnet image sets 8080), so Kestrel does not
  warn that `UseUrls` overrides it.
- Added: `Dockerfile`, `compose.yaml`, `.dockerignore`, a compact `.gitignore` (the stock
  `dotnet new gitignore` ignores every `bin/` — including the fleet's), `.env.example`,
  `fleet.conf`, `bin/`, `.github/workflows/`, `docs/fleet-lifecycle.md`.
- No NuGet lock file: the generator does not create one.

Known, by design: the container logs a DataProtection warning that its keys live inside the
container. Antiforgery tokens and circuits survive only as long as the container; persist the
key ring (a volume, Redis, or a database) before relying on that across restarts or replicas.

## Verified

**The docker runtime has NOT been verified yet.** On 2026-10-05 the shared docker host's disk
stayed at 0-5G free (under the 6G floor for a build) for over five hours, so `docker compose build`
was never run for this repo. Run the checks below once before trusting the image.

What did pass, inside `mcr.microsoft.com/dotnet/sdk:10.0` (.NET SDK 10.0.401):

- `FLEET_RUNTIME=process PORT=46204 bin/run` → `/health` 200, `/` renders (`<title>Home</title>`),
  `/counter` 200.
- `migrate.py audit` → `READY`.

Still to run: `verify.sh <repo> <port>` (bin/run → /health 200, bin/restart, bin/stop → no containers).
