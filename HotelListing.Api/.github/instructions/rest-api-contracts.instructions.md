
---

# 4. `.github/instructions/rest-api-contracts.instructions.md`

## High-level overview

This file guides Copilot on REST API design, versioning, routing, DTOs, status codes, pagination, validation, and OpenAPI/Swagger.

It is important because this project is an API teaching/demo repo. Endpoint design should be consistent, clean, and professional.

```markdown
---
applyTo: "HotelListing.Api/Controllers/**/*.cs,HotelListing.Api.Application/**/*Dto.cs,HotelListing.Api.Application/**/*.cs,HotelListing.Api/**/*Swagger*.cs,HotelListing.Api/**/*OpenApi*.cs"
---

# REST API Contract and OpenAPI Instructions

Use these instructions when adding or modifying controllers, endpoints, DTOs, route templates, API versioning, response types, Swagger/OpenAPI metadata, or API examples.

## General API design

- Design endpoints around resources and workflows.
- Use plural resource names for collections.
- Keep route templates predictable.
- Keep API versioning consistent.
- Prefer explicit binding attributes:
  - `[FromRoute]`
  - `[FromBody]`
  - `[FromQuery]`
  - `[FromHeader]`
- Do not expose EF Core entities directly as API request or response contracts.
- Use DTOs for request and response models.
- Keep DTO names clear and intention-revealing.

## Route and body consistency

- Route parameters are authoritative for nested resources.
- Avoid requiring clients to send the same ID in both route and body.
- If both route ID and body ID exist, validate that they match.
- For nested routes such as `/api/v{version}/hotels/{hotelId}/bookings`, use the route `hotelId` as the source of truth.
- Do not trust body IDs over route IDs.
- Do not allow a request body to move a resource under a different parent unless the endpoint explicitly supports that workflow.

## HTTP status codes

Use status codes consistently:

- `200 OK` for successful reads and updates that return content.
- `201 Created` for successful creates that return a location.
- `204 No Content` for successful deletes or updates with no response body.
- `400 Bad Request` for malformed or invalid requests.
- `401 Unauthorized` for unauthenticated requests.
- `403 Forbidden` for authenticated users without permission.
- `404 Not Found` when a resource does not exist or should not be revealed.
- `409 Conflict` for state conflicts such as duplicate data or invalid workflow transitions.
- `422 Unprocessable Entity` only when the request is syntactically valid but semantically invalid and this convention is intentionally adopted.
- `429 Too Many Requests` for rate-limited requests.
- `500 Internal Server Error` only for unexpected server failures.

## Error responses

- Prefer `ProblemDetails` for error responses.
- Keep error responses consistent.
- Do not return raw exception messages to clients outside Development.
- Include useful validation messages.
- Do not expose secrets, SQL details, stack traces, or implementation internals.
- Use model validation for request DTOs.
- Use service-level validation for business rules.

## Created responses

When creating resources:

- Use `CreatedAtAction`, `CreatedAtRoute`, or a correct equivalent.
- Ensure route values match the actual GET endpoint.
- Ensure API version route values are included when needed.
- Ensure nested route values such as `hotelId` are included when needed.
- Return a response DTO, not a domain entity.

## Pagination, filtering, and sorting

- Use pagination for collection endpoints that can grow.
- Keep pagination parameters consistent.
- Validate page size and page number.
- Enforce a maximum page size.
- Use query parameters for filtering and sorting.
- Do not return unbounded datasets from public endpoints.
- Document pagination metadata if returned.

## Versioning

- Keep versioned route patterns consistent.
- Do not introduce a new version unless behavior or contract meaningfully changes.
- Keep Swagger documents aligned with supported API versions.
- Do not silently change the response shape of an existing version.
- Document version-specific differences.

## Swagger/OpenAPI

- Keep Swagger metadata synchronized with actual endpoint behavior.
- Add response type metadata for important endpoints.
- Document authentication requirements.
- Document required headers such as `Authorization` and `X-Api-Key`.
- Add examples for complex request and response DTOs when useful.
- Ensure Swagger does not expose real secrets or production values.
- Do not include real JWTs or API keys in examples.
- Keep XML comments accurate.

## Controller design

- Keep controller actions small.
- Push workflow logic into application services.
- Validate route/body consistency before calling services.
- Do not query DbContext directly from controllers unless explicitly justified.
- Do not duplicate business rules across controllers.
- Use dependency injection.
- Avoid using `dynamic` or loosely typed responses.