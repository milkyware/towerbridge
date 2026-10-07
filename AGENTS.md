# AGENTS.md

## What this is

TowerBridge API: an ASP.NET Core minimal API (`net10.0`) that screen-scrapes Tower Bridge
lift times from <https://www.towerbridge.org.uk/lift-times> with HtmlAgilityPack and caches
the fetched HTML with LazyCache. One app project plus one test project — no monorepo here.

## Layout

- `src/TowerBridge.API/` — the app. Minimal API endpoints live in `Program.cs`. All DI
  registration is in `Extensions/ServiceCollectionExtensions.cs`, which is deliberately
  declared in namespace `Microsoft.Extensions.DependencyInjection` so `AddTowerBridgeService`
  is discovered like a framework extension.
- `tests/TowerBridge.Tests/` — NUnit 5 + NSubstitute. Tests run against stored HTML, not the live site.
- `TowerBridge.slnx` — XML solution format (not `.sln`).

## Commands

- Build: `dotnet build TowerBridge.slnx`
- Test all: `dotnet test TowerBridge.slnx`
- Single class (NUnit, filter on `FullyQualifiedName`):
  `dotnet test tests/TowerBridge.Tests/TowerBridge.Tests.csproj --filter "FullyQualifiedName~TowerBridgeServiceTests"`
- Run locally: `dotnet run --project src/TowerBridge.API` (Swagger at `/swagger`)
- Container: `docker build -t milkyware/towerbridge .` — the `publish` target depends on `test`,
  which depends on `build`, so **a green image build also runs and passes the tests**.
- Local compose: `docker compose up --build`.

## API surface

Minimal API endpoints in `Program.cs`, all under `/api/bridgelifts`: all lifts, `/next`, `/today`.
Model: `BridgeLift { Date, Vessel, Direction }`; `Date` is parsed from the page
`<time datetime="...">` attribute.

## Testing gotchas

- Fixtures are captured HTML in `tests/TowerBridge.Tests/Samples/*.html`, embedded through
  `Properties/Resources.resx` and read as `Resources.BridgeLiftsScheduled` / `BridgeLiftsNonScheduled`.
  Reading or changing parsing means updating the `.html` sample and the expected counts/rows together.
- The service takes `IDateTimeService`, so tests control "now"/"today"; production uses
  `DateTime.Now`/`DateTime.Today`.
- **Passing tests do not prove the live endpoint works.** Selectors in `TowerBridgeService`
  are checked against frozen samples, so if the real site changes the suite stays green while
  `/api/bridgelifts` fails (see Known bugs).

## Configuration & runtime

- `TOWERBRIDGE__CACHINGEXPIRATION` (TimeSpan, default `01:00:00`) binds to
  `TowerBridgeOptions.CachingExpiration`, which is the LazyCache entry TTL.
- Time zone is real: the service uses `DateTime.Now`/`DateTime.Today`, so the image needs tzdata.
  Keep the `-extra` base image (`mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra`);
  plain chiseled has no tzdata/ICU and silently falls back to UTC. Dockerfile defaults `TZ=UTC`,
  docker-compose sets `TZ=Europe/London`.
- The container listens on **8080** (base image sets `ASPNETCORE_HTTP_PORTS=8080`), not 80.

## Known bugs (do not assume these work)

- **Serilog is configured but never wired.** `appsettings.json` has a full `Serilog` section and
  the packages/`using Serilog` are present, but `builder.Host.UseSerilog()` is never called, so
  `/logs/log.txt` is never written and the Serilog sinks/levels are inert — logging goes through
  the default Microsoft logger.
- `/api/bridgelifts` returns HTTP 500 because the screen-scraper's HTML selectors no longer match
  the live towerbridge.org.uk page. (Current branch: `fix/bridge-lift-parsing`.)

## Releases & CI

- Commit and PR titles must be Conventional Commits — the `validate-pr-title` workflow enforces
  the format and labels the PR.
- release-please: `release-type: simple` with `include-component-in-tag: false`, so tags are
  `vX.Y.Z`. The version is a marker-wrapped `<Version>` in `src/TowerBridge.API/TowerBridge.API.csproj`
  (`x-release-please-start-version` / `end`) and is bumped automatically — do not hand-edit it.
- A release triggers `release-docker.yml`, which pushes multi-arch images to Docker Hub, GHCR and Quay.
- `.github/actions/` holds **vendored local composite actions** (`short-sha`, `label-pr`,
  `validate-pr-title`). `milkyware/towerbridge` is public while `milkyware/actions` is private, so
  workflows cannot reference `milkyware/actions/*` — add a local action instead.
- Workflow files are `ci-docker.yml` / `release-docker.yml`, but `TowerBridge.slnx` and the
  `ci-docker.yml` path filters still reference the old names `docker_ci.yml` / `docker_release.yml`;
  editing `ci-docker.yml` itself will not trigger Docker CI.

## Tooling notes

- No `Directory.Build.props`, `.editorconfig`, `global.json`, or lint step. `Nullable` is enabled and
  the build emits CS8618/CS8602 warnings; CI only fails on test failures and Trivy CRITICAL/HIGH, so
  do not treat warnings as blockers unless asked.
- `.vscode/launch.json` still points at `bin/Debug/net6.0`; prefer `dotnet run` (the target is net10.0).
