
---
# 2. `.github/instructions/dotnet10-api.instructions.md`

## High-level overview

This instruction file applies to .NET project files, C# source, startup configuration, JSON settings, and workflow files. It tells Copilot to keep the project aligned with **.NET 10 Web API best practices**.

It is especially useful when Copilot edits `Program.cs`, `*.csproj`, `global.json`, GitHub Actions, dependency injection setup, or package references.

```markdown
---
applyTo: "**/*.{cs,csproj,sln,props,targets,json,yml,yaml}"
---

# .NET 10 API Development Instructions

Use these instructions when modifying .NET project files, C# source files, startup configuration, package references, build files, or workflow files.

## Target framework

- Keep the solution aligned with .NET 10.
- Use `net10.0` for project target frameworks unless explicitly instructed otherwise.
- Keep nullable reference types enabled.
- Keep implicit usings enabled.
- Prefer SDK-style project files.
- Avoid adding unnecessary package references when ASP.NET Core already provides the capability.

## C# development style

- Prefer clear, readable C# over clever syntax.
- Use async APIs for database, identity, HTTP, file, and I/O operations.
- Add `CancellationToken` support to new controller actions and application service methods when appropriate.
- Prefer guard clauses for invalid inputs.
- Avoid broad `catch` blocks unless they add meaningful handling.
- Do not swallow exceptions silently.
- Use records for immutable DTOs when appropriate, but do not refactor existing DTOs unnecessarily.

## Startup and dependency injection

- Preserve the minimal hosting model in `Program.cs`.
- Do not reintroduce legacy `Startup.cs` unless explicitly requested.
- Keep service registration intentional and grouped.
- Prefer extension methods to organize large startup configuration sections.
- Keep middleware order correct and explicit.
- Do not register duplicate services unless there is a clear reason.
- Avoid service locator patterns.
- Prefer constructor injection.

## Package and dependency management

- Keep ASP.NET Core, EF Core, Identity, testing, and supporting packages compatible with .NET 10.
- Avoid mixing incompatible major versions.
- Do not introduce packages without clear value.
- Prefer built-in framework features over third-party packages for common platform concerns.
- Document any new infrastructure dependency.

## API project expectations

- Controllers should remain thin.
- Application services should own workflow/business logic.
- Domain and persistence concerns should remain outside controllers.
- Use DTOs for request and response contracts.
- Use `ProblemDetails` for standardized errors.
- Keep API versioning behavior consistent.
- Keep Swagger/OpenAPI configuration synchronized with endpoint behavior.

## Validation before completion

After modifying platform, project, package, startup, or workflow files, validate with:

```bash
dotnet restore HotelListing.Api.sln
dotnet build HotelListing.Api.sln --configuration Release --no-restore
dotnet test HotelListing.Api.sln --configuration Release --no-build