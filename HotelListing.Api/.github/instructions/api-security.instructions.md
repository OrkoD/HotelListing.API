
---

# 3. `.github/instructions/api-security.instructions.md`

## High-level overview

This is the most important instruction file for this repo. It guides Copilot whenever authentication, authorization, configuration, JWT, Basic auth, API keys, Identity, secrets, or middleware are involved.

This file directly addresses the current concerns: authentication schemes are registered in startup, but the pipeline needs strict authentication/authorization ordering; the current appsettings includes a committed API key, JWT signing key, and SQL connection using `Encrypt=false`. :contentReference[oaicite:3]{index=3}

```markdown
---
applyTo: "HotelListing.Api/**/*.{cs,json},HotelListing.Api.Application/**/*.cs,HotelListing.Api.Common/**/*.cs,.github/workflows/*.{yml,yaml}"
---

# API Security Instructions

Use these instructions when modifying authentication, authorization, Identity, JWT, Basic authentication, API-key authentication, appsettings, secrets, logging, middleware, controllers, or deployment workflows.

## Authentication middleware

- Always call `app.UseAuthentication()` before `app.UseAuthorization()`.
- Never remove authentication middleware from the pipeline when authenticated endpoints exist.
- Keep authentication scheme selection intentional and documented.
- Ensure custom authentication handlers produce consistent claims.

Required order:

```csharp
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();