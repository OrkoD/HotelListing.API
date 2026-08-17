---
name: api-auth-hardening
description: Use this skill when reviewing or changing ASP.NET Core Web API authentication, authorization, JWT, Identity, Basic auth, API keys, roles, claims, secrets, appsettings, or security-sensitive middleware.
---

# API Authentication and Authorization Hardening Skill

Use this skill when the task involves authentication, authorization, JWT Bearer tokens, ASP.NET Core Identity, Basic authentication, API-key authentication, role assignment, claims, appsettings secrets, security-sensitive logging, or protected API endpoints.

## Primary objectives

- Ensure authentication middleware runs before authorization middleware.
- Ensure JWT validation remains strict.
- Prevent anonymous users from assigning themselves privileged roles.
- Protect administrative endpoints.
- Protect user-specific resources through ownership checks.
- Remove committed secrets.
- Keep API keys, JWT signing keys, passwords, and connection strings out of source control.
- Ensure logs never expose sensitive values.
- Ensure Swagger/OpenAPI does not contain real secrets.

## Files to inspect

Inspect these files and folders when relevant:

- `Program.cs`
- Authentication handlers
- Authorization policies
- Auth controllers
- User/Identity services
- Booking controllers and services
- Admin controllers
- `appsettings*.json`
- GitHub Actions workflows
- Swagger/OpenAPI configuration
- Logging configuration

## Middleware checklist

Confirm the application pipeline includes:

```csharp
app.UseAuthentication();
app.UseAuthorization();