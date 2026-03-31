# Repository Guidelines

## Project Structure & Module Organization
`StatusPageSharp.slnx` groups the solution into `src/` and `tests/`. Keep feature code in the matching layer:

- `src/StatusPageSharp.Domain`: entities, enums, and pure domain logic.
- `src/StatusPageSharp.Application`: service abstractions and DTO-style models.
- `src/StatusPageSharp.Infrastructure`: EF Core, Identity, migrations, monitoring, and service implementations.
- `src/StatusPageSharp.Web`: Razor Pages UI, API endpoints, static assets in `wwwroot/`.
- `src/StatusPageSharp.Worker`: background monitoring host.
- `tests/StatusPageSharp.*.Tests`: xUnit coverage for domain and infrastructure behavior.

## Build, Test, and Development Commands
- `dotnet restore StatusPageSharp.slnx`: restore NuGet packages for the full solution.
- `dotnet build StatusPageSharp.slnx`: build every project; prefer solution builds over project-by-project builds.
- `dotnet test StatusPageSharp.slnx`: run all xUnit tests.
- `dotnet run --project src/StatusPageSharp.Web`: start the web app locally (`https://localhost:7197`, `http://localhost:5008`).
- `dotnet run --project src/StatusPageSharp.Worker`: start the monitoring worker.
- `dotnet ef database update --project src/StatusPageSharp.Infrastructure --startup-project src/StatusPageSharp.Web`: apply EF Core migrations for local development.

## Coding Style & Naming Conventions
Use 4 spaces for indentation and file-scoped namespaces. Follow the existing C# style: explicit access modifiers, `PascalCase` for types and members, `var` for local variables, collection expressions like `[]`, and xUnit-style test names such as `MethodName_ExpectedResult_WhenCondition`. Keep one type per file and place new code in the layer that owns the behavior.

## Testing Guidelines
Tests use xUnit with `coverlet.collector`. Add unit tests beside the affected layer, and prefer deterministic tests with the existing helpers under `tests/StatusPageSharp.Infrastructure.Tests/Support`. Cover domain rules, service behavior, and regression cases before opening a PR.

## Configuration & Security Tips
Local settings live in each app's `appsettings*.json`. The web app expects a `DefaultConnection` Postgres connection and bootstraps the database on startup; development also seeds the bootstrap admin account. Do not commit real credentials or production connection strings.

## Commit & Pull Request Guidelines
History is minimal, but the existing convention uses short, imperative subjects like `Initial commit`. Keep commits focused and descriptive, explain behavior changes in the PR body, link the relevant issue, and include screenshots for UI changes under `src/StatusPageSharp.Web`.
