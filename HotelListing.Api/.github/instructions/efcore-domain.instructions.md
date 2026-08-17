
---

# 6. `.github/instructions/efcore-domain.instructions.md`

## High-level overview

This file guides Copilot whenever it touches EF Core entities, DbContext, migrations, service queries, indexes, relationships, tracking, and data persistence.

It is particularly useful because this API has a domain project with the DbContext, migrations, Identity integration, and domain entities.

```markdown
---
applyTo: "HotelListing.Api.Domain/**/*.cs,HotelListing.Api.Application/Services/**/*.cs,HotelListing.Api.Application/Contracts/**/*.cs"
---

# EF Core and Domain Data Instructions

Use these instructions when modifying entities, DbContext, entity configurations, migrations, repositories, application services, and data-access queries.

## General EF Core rules

- Use async EF Core APIs.
- Use `CancellationToken` in new async data-access methods.
- Use `AsNoTracking()` for read-only queries.
- Use projection into DTOs for API responses.
- Avoid returning EF Core entities directly from API endpoints.
- Avoid loading entire object graphs when projection is sufficient.
- Avoid N+1 query patterns.
- Avoid client-side evaluation of large datasets.
- Do not concatenate SQL with user input.
- Use raw SQL only when it is justified and parameterized.

## Query design

For read operations:

- Filter as early as possible.
- Project only needed fields.
- Use pagination for large collections.
- Use `AsNoTracking()` unless changes will be saved.
- Avoid unnecessary `Include` statements before projection.
- Avoid `ToListAsync()` before filtering, sorting, or paging.
- Keep query intent clear.

For write operations:

- Load only the records needed for validation and mutation.
- Validate business rules before saving.
- Use transactions when a workflow requires atomicity.
- Avoid updating fields that should be server-controlled.
- Do not trust client-supplied identity, role, status, price, or ownership fields.

## Entity configuration

Use explicit configuration for:

- Required fields.
- Maximum string lengths.
- Decimal precision.
- Relationships.
- Delete behavior.
- Indexes.
- Unique constraints.
- Concurrency tokens where appropriate.

Prefer entity configuration classes for complex configuration.

## Migrations

When adding migrations:

- Use descriptive migration names.
- Review generated migration code.
- Avoid accidental data loss.
- Do not drop columns or tables unless explicitly intended.
- Do not seed real credentials, real API keys, or production secrets.
- Keep seed data safe and educational.
- Document migration commands if setup behavior changes.

Recommended commands:

```bash
dotnet ef migrations add DescriptiveMigrationName --project HotelListing.Api.Domain --startup-project HotelListing.Api
dotnet ef database update --project HotelListing.Api.Domain --startup-project HotelListing.Api