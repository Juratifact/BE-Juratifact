# Repository Guidelines

## Project Structure & Module Organization

- `Juratifact.sln`: solution entry point for local dev and CI.
- `Juratifact.API/`: ASP.NET Core Web API (controllers in `Controller/`, middleware in `Middlewares/`, app wiring in `Program.cs`).
- `Juratifact.Service/`: business/domain services grouped by feature (e.g., `Order/`, `Product/`, `Cart/`, `Notification/`).
- `Juratifact.Repository/`: EF Core data layer (`AppDbContext.cs`, entities in `Entity/`, migrations in `Migrations/`).
- `.github/workflows/deploy.yml` + `Dockerfile`: Docker build/push and Azure Web App deploy on pushes to `main`.

Note: there is also a `Juratifact/` folder that mirrors these projects; `Juratifact.sln` targets the top-level `Juratifact.*` directories.

## Build, Test, and Development Commands

```powershell
dotnet restore Juratifact.sln
dotnet build Juratifact.sln
dotnet run --project Juratifact.API
```

EF Core migrations (stored under `Juratifact.Repository/Migrations`):

```powershell
dotnet tool install --global dotnet-ef  # one-time, if needed
dotnet ef migrations add <Name> -p Juratifact.Repository -s Juratifact.API
dotnet ef database update -s Juratifact.API
```

Docker image (API listens on container port 8080):

```powershell
docker build -t juratifact-backend .
docker run -p 8080:8080 juratifact-backend
```

## Coding Style & Naming Conventions

- C#/.NET 8 (`net8.0`) with nullable reference types enabled.
- Indent with 4 spaces. Use `PascalCase` for public types/methods and `camelCase` for locals/parameters. Interfaces follow `I*` (e.g., `IOrderService`).
- Keep feature logic in `Juratifact.Service/<Feature>/` and wire DI in `Juratifact.API/Program.cs`.

## Testing Guidelines

- The solution currently has no dedicated `*.Tests` project. If you introduce tests, add a separate test project (e.g., `Juratifact.Tests`) and keep tests deterministic and fast.
- Run the full suite with `dotnet test Juratifact.sln` once test projects exist.

## Commit & Pull Request Guidelines

- Prefer Conventional Commit-style subjects used in recent history: `feat(scope): ...`, `fix(scope): ...`, `chore: ...` (example: `feat(order): allow shipping address updates`).
- Open PRs against `dev` for feature work; keep `main` deployable (GitHub Actions builds/pushes the Docker image on pushes to `main`).
- PR description should call out API contract changes, required migrations, and any config/env var updates. Include example requests/responses when adding or changing endpoints.

## Security & Configuration Tips

- Do not commit secrets (DB passwords, JWT keys, third-party tokens). Use environment variables and local-only overrides for development.
- Configuration lives in `Juratifact.API/appsettings.json` and `Juratifact.API/appsettings.Development.json`; prefer the development file for local settings.
