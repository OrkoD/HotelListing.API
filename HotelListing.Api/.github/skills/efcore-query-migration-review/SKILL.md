---
name: efcore-query-migration-review
description: Use this skill when reviewing or changing EF Core queries, entities, DbContext configuration, migrations, indexes, relationships, delete behavior, projections, tracking behavior, transactions, and data integrity.
---

# EF Core Query and Migration Review Skill

Use this skill when the task involves EF Core queries, application service data access, entity classes, entity configurations, DbContext setup, migrations, indexes, relationships, transactions, or data-integrity rules.

## Primary objectives

- Use async EF Core APIs.
- Use `AsNoTracking()` for read-only queries.
- Project into DTOs for API responses.
- Avoid exposing EF Core entities directly through API contracts.
- Avoid unnecessary `Include` usage.
- Avoid N+1 query patterns.
- Avoid unbounded collection responses.
- Configure entities explicitly.
- Review migrations for safety.
- Add indexes where query patterns justify them.
- Protect booking workflow data integrity.
- Use transactions when a workflow must be atomic.

## Files to inspect

Inspect these files and folders when relevant:

- DbContext
- Entity classes
- Entity configuration classes
- Migrations
- Application services
- Service contracts
- AutoMapper profiles
- DTOs
- Query/filter/paging models
- Integration tests

## Read query checklist

For read-only queries:

1. Filter as early as possible.
2. Sort before paging.
3. Page before materialization.
4. Use `AsNoTracking()`.
5. Project into DTOs.
6. Avoid `Include` when projection is enough.
7. Avoid `ToListAsync()` before filtering, sorting, or paging.
8. Avoid returning unbounded datasets.
9. Avoid loading full object graphs unnecessarily.
10. Use cancellation tokens where available.

## Write workflow checklist

For write operations:

1. Load only the records needed.
2. Validate business rules before mutation.
3. Do not trust client-controlled fields.
4. Do not trust client-supplied user ID, role, status, or price.
5. Use `SaveChangesAsync`.
6. Use transactions for multi-step operations that must commit atomically.
7. Consider concurrency where multiple users may update related data.
8. Return DTOs, not entities.

## Entity configuration checklist

Review configuration for:

- Required fields.
- Maximum string lengths.
- Decimal precision.
- Relationships.
- Delete behavior.
- Indexes.
- Unique constraints.
- Concurrency tokens where appropriate.
- Identity-related normalization fields.
- Booking-related date/status/user/hotel indexes.

## Migration checklist

When reviewing a migration:

- Confirm the migration name describes the change.
- Check for accidental table drops.
- Check for accidental column drops.
- Check for unsafe data transformations.
- Check for unsafe seed data.
- Check for missing indexes.
- Check decimal precision.
- Check relationship changes.
- Check cascade delete behavior.
- Confirm the migration matches the intended domain change.

## Indexing guidance

Consider indexes for frequently queried fields:

- Hotel ID.
- Country ID.
- User ID.
- Booking status.
- Check-in date.
- Check-out date.
- API key lookup fields.
- Normalized Identity fields.

Do not add indexes blindly. Justify indexes by query behavior.

## Booking persistence checklist

For booking-related persistence:

- Validate hotel existence.
- Validate authenticated user ownership.
- Validate date range.
- Calculate total price server-side.
- Persist calculated total price.
- Validate status transitions.
- Avoid race conditions in availability checks.
- Use transactions where availability check and booking creation must be atomic.

## Test expectations

Add or recommend tests for:

- Query filtering.
- Pagination.
- Projection behavior.
- Booking creation.
- Booking status changes.
- Authorization-sensitive data access.
- Migration-sensitive behavior where practical.
- Data integrity constraints.