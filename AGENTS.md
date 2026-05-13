# Repository Guidelines

## Project Structure & Module Organization

This repository is a .NET 8 backend solution. The active solution file is `Juratifact.sln`, which includes:

- `Juratifact.API/`: ASP.NET Core entry point, controllers, middleware, Swagger/JWT setup, and `appsettings*.json`.
- `Juratifact.Service/`: business logic grouped by feature, for example `Product/`, `Order/`, `Wallet/`, plus background Quartz jobs and external services.
- `Juratifact.Repository/`: EF Core `AppDbContext`, entities, enums, abstractions, and migrations.
- `.github/workflows/deploy.yml` and `Dockerfile`: Docker image build and Azure Web App deployment.

Ignore generated `bin/` and `obj/` files. The nested `Juratifact/` directory appears to be an older duplicate scaffold; prefer the root project folders unless the solution is changed.

## Build, Test, and Development Commands

- `dotnet restore Juratifact.sln`: restore NuGet packages.
- `dotnet build Juratifact.sln`: compile all projects.
- `dotnet run --project Juratifact.API/Juratifact.API.csproj --launch-profile http`: run the API locally at `http://localhost:5028` with Swagger at `/swagger`.
- `dotnet ef migrations add <Name> --project Juratifact.Repository --startup-project Juratifact.API`: add EF Core migrations.
- `docker build -t juratifact-api .`: build the production container image.

## Coding Style & Naming Conventions

Use C# conventions already present in the codebase: 4-space indentation, file-scoped namespaces, PascalCase for public types and members, camelCase for locals and parameters, and `_camelCase` for private fields. Keep interfaces prefixed with `I` such as `IProductService`. Place feature request/response DTOs beside their service folder, commonly as `Request.cs` and `Response.cs`. Keep controller routes explicit, for example `[Route("api/products")]`.

## Testing Guidelines

No test project is currently committed. When adding tests, create a separate project such as `Juratifact.Tests`, use xUnit or NUnit consistently, and name files after the subject under test, for example `ProductServiceTests.cs`. Run tests with `dotnet test`. Cover service logic, authorization-sensitive controller behavior, and EF queries that affect orders, payments, wallets, and disputes.

## Commit & Pull Request Guidelines

Recent history uses Conventional Commit style, for example `feat(video): upload video`, `feat(order): checkout selected cart items`, and `feat: implement ProductController...`. Continue using `type(scope): summary` where practical.

Pull requests should include a short description, affected endpoints or services, database migration notes, local verification commands, and linked issues. Include screenshots only when Swagger output, API responses, or deployment logs clarify the change.

## Security & Configuration Tips

Do not add new secrets to `appsettings.json`. Use environment variables, user secrets, CI secrets, or Azure App Service settings for connection strings, JWT keys, Cloudinary, mail, SePay, and Discord webhook values.
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
