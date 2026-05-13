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
