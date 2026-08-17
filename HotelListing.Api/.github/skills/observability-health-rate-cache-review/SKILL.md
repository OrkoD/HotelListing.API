---
name: observability-health-rate-cache-review
description: Use this skill when reviewing or changing structured logging, Serilog, exception handling, ProblemDetails, health checks, HealthChecks UI, rate limiting, output caching, diagnostics, or production-readiness behavior.
---

# Observability, Health, Rate Limit, and Cache Review Skill

Use this skill when the task involves logging, Serilog, exception handling, `ProblemDetails`, diagnostics, health checks, HealthChecks UI, rate limiting, output caching, performance protection, or production-readiness behavior.

## Primary objectives

- Keep logs structured and useful.
- Prevent sensitive data from appearing in logs.
- Use centralized exception handling.
- Return safe `ProblemDetails` responses.
- Separate liveness and readiness when appropriate.
- Prevent health endpoints from leaking sensitive details.
- Ensure rate limiting protects sensitive and expensive endpoints.
- Ensure output caching does not leak user-specific data.
- Keep production diagnostics safe.
- Document operational behavior.

## Files to inspect

Inspect these files and areas when relevant:

- `Program.cs`
- Logging configuration
- Serilog setup
- Exception handling middleware
- `ProblemDetails` setup
- Health check registration
- Health check endpoint mapping
- HealthChecks UI configuration
- Rate limiting policies
- Output caching policies
- Controllers with cache attributes
- Authentication and registration endpoints
- README operational documentation

## Logging checklist

Do not log:

- JWTs.
- API keys.
- Passwords.
- Authorization headers.
- Full connection strings.
- Refresh tokens.
- Sensitive request bodies.
- Sensitive personal data unless explicitly justified.

Prefer structured logging fields such as:

- Request ID.
- HTTP method.
- Route.
- Status code.
- Elapsed time.
- Operation name.
- Safe user identifier when appropriate.

## Serilog checklist

- Keep Serilog configuration environment-aware.
- Avoid verbose logging in production.
- Do not enable EF Core sensitive data logging outside Development.
- Use appropriate log levels.
- Use bootstrap logging for startup failures where appropriate.
- Avoid logging sensitive payloads.

## Exception handling checklist

- Use centralized exception handling.
- Return `ProblemDetails` for API errors.
- Do not expose stack traces outside Development.
- Do not expose SQL details.
- Do not expose authentication internals.
- Convert known domain failures into appropriate HTTP responses.
- Log unexpected exceptions with safe context.

## Health check checklist

- Separate liveness and readiness if useful.
- Liveness should prove the process is running.
- Readiness should check required dependencies.
- Do not expose raw dependency errors publicly.
- Do not expose connection strings.
- Restrict HealthChecks UI in production when needed.
- Document health endpoint behavior.

Recommended endpoint semantics:

```text
/healthz/live   - process liveness
/healthz/ready  - dependency readiness
/healthz        - aggregate health check, if intentionally exposed