# GitHub Copilot Instructions — HotelListing.Api

## Project identity

This repository contains a .NET 10 ASP.NET Core Web API for managing hotels, countries, bookings, users, roles, authentication, and administrative workflows.

Treat this as a production-style educational API. Code should be clear enough for learners to understand, but strong enough to demonstrate professional API engineering practices.

The application uses a layered structure:

- `HotelListing.Api`
  - ASP.NET Core Web API host.
  - Controllers, middleware, authentication handlers, Swagger/OpenAPI setup, rate limiting, output caching, health checks, and startup configuration.
- `HotelListing.Api.Application`
  - Application services, service contracts, DTOs, mapping profiles, and business workflows.
- `HotelListing.Api.Domain`
  - EF Core DbContext, Identity user, domain entities, entity configurations, migrations, and persistence concerns.
- `HotelListing.Api.Common`
  - Shared constants, configuration models, reusable helper types, and cross-cutting models.
- `HotelListing.Api.IntegrationTests`
  - Integration tests and API validation tests.

Preserve this separation unless explicitly asked to redesign the architecture.

## Target platform

- Use .NET 10 for all new development.
- Use `net10.0` as the target framework unless a task explicitly requires another framework.
- Keep nullable reference types enabled.
- Keep implicit usings enabled.
- Prefer modern C# syntax when it improves readability.
- Use async APIs for database, authentication, identity, service, and I/O operations.
- Add `CancellationToken` parameters to new async application service methods and controller actions when appropriate.

## Architecture rules

- Controllers should handle HTTP concerns only.
- Business rules belong in application services.
- EF Core persistence concerns belong in the domain/data layer or application service layer, not controllers.
- Do not expose EF Core entities directly as API request models.
- Use DTOs for API requests and responses.
- Keep mapping logic centralized through AutoMapper profiles or explicit mapping helpers.
- Avoid large controller actions.
- Avoid duplicating business rules across controllers.
- Prefer clear service contracts over direct DbContext access from controllers.

## API design rules

- Design endpoints around resources and workflows.
- Use explicit route, body, and query binding where it improves clarity.
- Route parameters should be treated as authoritative for nested resources.
- Avoid accepting duplicate IDs from both route and body unless they are validated for consistency.
- Return appropriate HTTP status codes.
- Use `ProblemDetails` for error responses.
- Validate input before processing business workflows.
- Use pagination for list endpoints that can grow.
- Keep response shapes stable and documented.
- Do not return stack traces or raw exception details to API clients.

## Authentication and authorization

- Always call `app.UseAuthentication()` before `app.UseAuthorization()`.
- Do not weaken JWT validation settings.
- Do not allow anonymous users to assign themselves privileged roles.
- Protect administrative endpoints with role-based or policy-based authorization.
- Protect user-specific resources by validating ownership.
- Do not rely on Swagger, UI visibility, or client behavior for security.
- Authorization must be enforced server-side.
- Keep authentication schemes intentional and documented.
- Treat Basic authentication as demo-only unless a production justification is explicitly documented.
- Treat API keys as secrets.
- Do not commit real API keys, JWT signing keys, passwords, tokens, or production connection strings.

## Configuration and secrets

- Never commit secrets.
- Do not store production credentials in `appsettings.json`.
- Use local user secrets for development secrets.
- Use environment variables, Azure App Settings, managed identity, or Azure Key Vault for deployed environments.
- Avoid `Encrypt=false` in production SQL connection strings.
- Keep committed configuration files safe by using placeholders or non-sensitive defaults.
- Do not log secrets, tokens, connection strings, API keys, passwords, or authorization headers.

## EF Core and data access

- Use async EF Core methods.
- Use `AsNoTracking()` for read-only queries.
- Use projection into DTOs for list/detail responses when tracking is unnecessary.
- Avoid over-fetching large object graphs.
- Avoid N+1 query patterns.
- Keep entity configuration explicit for relationships, required fields, precision, lengths, indexes, and delete behavior.
- Use migrations intentionally.
- Do not concatenate SQL strings with user input.
- Use transactions for multi-step workflows that must commit atomically.
- Validate booking rules before saving changes.

## Booking workflow rules

Booking logic is domain-critical.

When creating, updating, confirming, or cancelling bookings:

- Validate hotel existence.
- Validate user identity and ownership.
- Validate check-in and check-out dates.
- Reject zero-night or negative-night bookings.
- Use the route `hotelId` as the source of truth for nested hotel booking routes.
- Validate booking overlap rules.
- Calculate and persist `TotalPrice`.
- Validate allowed booking status transitions.
- Prevent users from modifying bookings they do not own.
- Restrict administrative booking operations to authorized roles or policies.

## Observability and reliability

- Use structured logging.
- Log useful operational context without exposing sensitive data.
- Use health checks carefully.
- Do not expose sensitive database diagnostics through public health endpoints.
- Use rate limiting policies intentionally.
- Ensure output caching does not leak user-specific or role-specific data.
- Prefer liveness and readiness endpoints with clear semantics.

## Testing expectations

For meaningful changes:

- Add or update tests.
- Prefer integration tests for API endpoints, authentication, authorization, validation, and booking workflows.
- Use unit tests for application service business rules where appropriate.
- Test happy paths, validation failures, authorization failures, and edge cases.
- Do not claim completion until restore, build, and tests pass.

## CI expectations

Use solution-level commands where possible:

```bash
dotnet restore HotelListing.Api.sln
dotnet build HotelListing.Api.sln --configuration Release --no-restore
dotnet test HotelListing.Api.sln --configuration Release --no-build
```
