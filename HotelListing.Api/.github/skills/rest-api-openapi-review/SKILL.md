---
name: rest-api-openapi-review
description: Use this skill when reviewing or changing REST API routes, controllers, DTOs, API versioning, status codes, ProblemDetails, pagination, Swagger/OpenAPI metadata, examples, or API documentation.
---

# REST API and OpenAPI Review Skill

Use this skill when the task involves API controllers, route templates, DTOs, status codes, error responses, API versioning, Swagger/OpenAPI metadata, XML comments, request/response examples, authentication documentation, pagination, filtering, or sorting.

## Primary objectives

- Keep route design consistent.
- Keep API versioning consistent.
- Use DTOs for request and response contracts.
- Avoid exposing EF Core entities directly.
- Use appropriate HTTP status codes.
- Use `ProblemDetails` for API errors.
- Ensure create endpoints return correct `201 Created` behavior.
- Ensure route/body ID consistency.
- Use pagination for growing collections.
- Keep Swagger/OpenAPI aligned with runtime behavior.
- Do not include real secrets in examples.

## Files to inspect

Inspect these files and folders when relevant:

- Controllers
- DTOs
- API versioning setup
- Swagger/OpenAPI setup
- XML comments
- Response models
- Error handling middleware
- Validation filters
- README/API documentation
- Integration tests

## Route design checklist

- Use plural resource names for collections.
- Keep nested resource routes consistent.
- Treat route IDs as authoritative.
- Avoid duplicate IDs in route and body.
- If duplicate IDs exist, validate they match.
- Use explicit binding attributes where helpful:
  - `[FromRoute]`
  - `[FromBody]`
  - `[FromQuery]`
  - `[FromHeader]`
- Avoid route ambiguity.
- Keep route names stable when used by `CreatedAtAction` or `CreatedAtRoute`.

## Status code checklist

Use status codes consistently:

- `200 OK` for successful reads and updates with response content.
- `201 Created` for successful resource creation.
- `204 No Content` for successful deletes or updates with no response body.
- `400 Bad Request` for invalid requests.
- `401 Unauthorized` for unauthenticated requests.
- `403 Forbidden` for authenticated users without permission.
- `404 Not Found` for missing resources.
- `409 Conflict` for business state conflicts.
- `422 Unprocessable Entity` only if intentionally adopted.
- `429 Too Many Requests` for rate-limited requests.
- `500 Internal Server Error` only for unexpected failures.

## Error response checklist

- Prefer `ProblemDetails`.
- Keep error response shape consistent.
- Do not expose stack traces outside Development.
- Do not expose SQL details.
- Do not expose secrets.
- Use model validation for request DTOs.
- Use service-level validation for business rules.
- Return meaningful validation messages.

## Created response checklist

For POST create endpoints:

- Return `201 Created` when a new resource is created.
- Use `CreatedAtAction`, `CreatedAtRoute`, or equivalent.
- Ensure route values match the GET endpoint.
- Include API version route values when needed.
- Include nested route values such as `hotelId` when needed.
- Return a response DTO, not a domain entity.

## Pagination checklist

For list endpoints:

- Use pagination where data can grow.
- Validate page number.
- Validate page size.
- Enforce a maximum page size.
- Apply filtering before paging.
- Apply sorting before paging.
- Document pagination metadata if returned.
- Do not return unbounded public collections.

## Swagger/OpenAPI checklist

- Document authentication schemes.
- Document required headers.
- Add response type metadata.
- Add safe examples where useful.
- Do not include real JWTs.
- Do not include real API keys.
- Do not include passwords.
- Do not include production connection strings.
- Keep Swagger docs aligned with runtime behavior.
- Keep versioned APIs visible and understandable.
- Keep XML comments accurate.

## Test expectations

Add or recommend tests for:

- Expected status codes.
- Validation error shape.
- `ProblemDetails` responses.
- Created response location.
- Versioned route behavior.
- Pagination behavior.
- Route/body ID mismatch.
- Swagger/OpenAPI generation where useful.