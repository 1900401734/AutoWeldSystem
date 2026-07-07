# Repository Guidelines

## Project Structure & Module Organization

`AutoWeldSystem.sln` is a .NET 8 Windows Forms solution. Keep domain rules, DTOs, entities, constants, and interfaces in `AutoWeldSystem.Core`. Put SqlSugar database context setup in `AutoWeldSystem.Data`. Business integrations and workflows belong in `AutoWeldSystem.Services`, grouped by areas such as `Mes`, `Plc`, `Production`, `Log`, and `Center`. UI forms, views, controls, assets, and WinForms infrastructure belong in `AutoWeldSystem.UI`; keep designer files focused on layout. `AutoWeldSystem.CenterServer` contains the ASP.NET Core dashboard/ingest server. Console regression tests live in `AutoWeldSystem.Tests/Program.cs`. Local DLLs are under `AutoWeldSystem.Libs`; supporting notes are in `docs` and root `*.md` files.

## Build, Test, and Development Commands

- `dotnet restore AutoWeldSystem.sln`: restore NuGet packages.
- `dotnet build AutoWeldSystem.sln --no-restore`: compile all projects.
- `dotnet build AutoWeldSystem.sln --no-restore -p:BaseOutputPath=..\artifacts\verify-bin\`: build into an alternate output directory when normal `bin` files are locked.
- `dotnet run --project AutoWeldSystem.Tests\AutoWeldSystem.Tests.csproj --no-restore`: run the console regression harness.
- `dotnet run --project AutoWeldSystem.UI\AutoWeldSystem.UI.csproj`: launch the WinForms client on Windows.
- `dotnet publish AutoWeldSystem.UI\AutoWeldSystem.UI.csproj -c Release -r win-x64 --self-contained false`: create a release build.

## Coding Style & Naming Conventions

Use C# with nullable reference types enabled. Prefer 4-space indentation, short methods, and simple control flow. Use PascalCase for classes, methods, properties, and constants; camelCase for locals and parameters; prefix interfaces with `I`; suffix asynchronous methods with `Async`. Keep reusable business decisions in rule/helper classes in `Core` instead of duplicating logic in UI handlers. Add clear comments for classes, public methods, and non-obvious key statements, but avoid comments that only repeat the code.

## Testing Guidelines

Add focused regression cases to `AutoWeldSystem.Tests/Program.cs` using descriptive names in the existing `(Name, Run)` list. Prefer pure rule/service tests that do not require PLC, MES, MySQL, or UI automation. Run the harness before full solution builds for behavioral changes.

## Commit & Pull Request Guidelines

Recent history uses concise Conventional Commit style, for example `feat(address): improve alarm import selection`, `test: add tests for plc debug rules`, and `chore: update system settings designer layout`. Keep commits scoped and describe the behavioral change. PRs should include a summary, validation commands, linked work item when available, and screenshots for UI changes.

## Security & Configuration Tips

Do not commit real `AutoWeldSystem.UI/appsettings.json`, database passwords, PLC addresses, MES endpoints, or machine-local paths. Use `AutoWeldSystem.UI/appsettings.example.json` as the template. Treat generated `bin`, `obj`, `.vs`, and publish artifacts as local output.
