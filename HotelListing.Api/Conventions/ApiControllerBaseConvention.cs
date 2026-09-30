using HotelListing.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;

namespace HotelListing.Api.Conventions;

public class ApiControllerBaseConvention : IActionModelConvention
{
    public void Apply(ActionModel action)
    {
        // 1. Only target controllers inheriting from ApiControllerBase
        if (!typeof(ApiControllerBase).IsAssignableFrom(action.Controller.ControllerType))
            return;

        // 2. Discover HTTP methods (from constraints or [HttpGet], [HttpPost], etc.)
        var httpMethods = action.Selectors
            .SelectMany(s => s.ActionConstraints?.OfType<HttpMethodActionConstraint>() ?? [])
            .SelectMany(c => c.HttpMethods)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (httpMethods.Count == 0)
        {
            var methodAttributes = action.Attributes.OfType<HttpMethodAttribute>().SelectMany(a => a.HttpMethods);
            foreach (var method in methodAttributes)
                httpMethods.Add(method);
        }

        var isMutating = httpMethods.Any(m => m is "POST" or "PUT" or "PATCH");
        var isLookupOrDelete = httpMethods.Any(m => m is "GET" or "PUT" or "PATCH" or "DELETE");

        // 3. Distinguish route-bound parameters (e.g. {id}) from query filters
        var hasRouteParameter = action.Parameters.Any(p =>
            p.BindingInfo?.BindingSource == BindingSource.Path
            || action.Selectors.Any(s => s.AttributeRouteModel?.Template?.Contains($"{{{p.ParameterName}}}") == true)
        );
        var hasParameters = action.Parameters.Count > 0;

        // 4. Success Response (200 OK with T, or 204 NoContent for void/IActionResult)
        var returnType = action.ActionMethod.ReturnType;
        if (returnType.IsGenericType && (returnType.GetGenericTypeDefinition() == typeof(Task<>) || returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ActionResult<>))
        {
            var successDtoType = returnType.GetGenericArguments()[0];
            AddFilterIfNotExists(action, successDtoType, StatusCodes.Status200OK);
        }
        else if (returnType == typeof(IActionResult) || returnType == typeof(ActionResult))
        {
            AddFilterIfNotExists(action, null, StatusCodes.Status204NoContent);
        }

        // 5. Global 500
        AddFilterIfNotExists(action, typeof(ProblemDetails), StatusCodes.Status500InternalServerError);

        // 6. 400 Bad Request / ValidationProblemDetails (applies to mutations or actions taking parameters)
        if (hasParameters || isMutating)
            AddFilterIfNotExists(action, typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest);

        // 7. 404 Not Found (ONLY for route-identified resources, never generic query-filtered lists)
        if (hasRouteParameter && isLookupOrDelete)
            AddFilterIfNotExists(action, typeof(ProblemDetails), StatusCodes.Status404NotFound);

        // 8. 409 Conflict (only for state mutations)
        if (isMutating)
            AddFilterIfNotExists(action, typeof(ProblemDetails), StatusCodes.Status409Conflict);

        // 9. 401 & 403 Authentication / Authorization
        var isAnonymous = action.Attributes.OfType<AllowAnonymousAttribute>().Any();
        var isAuthorized = !isAnonymous && (
            action.Attributes.OfType<AuthorizeAttribute>().Any() ||
            action.Controller.Attributes.OfType<AuthorizeAttribute>().Any());

        if (isAuthorized)
        {
            AddFilterIfNotExists(action, null, StatusCodes.Status401Unauthorized);
            AddFilterIfNotExists(action, null, StatusCodes.Status403Forbidden);
        }
    }

    private static void AddFilterIfNotExists(ActionModel action, Type? type, int statusCode)
    {
        if (action.Filters.OfType<ProducesResponseTypeAttribute>().Any(f => f.StatusCode == statusCode))
            return;

        var filter = type is not null
            ? new ProducesResponseTypeAttribute(type, statusCode)
            : new ProducesResponseTypeAttribute(statusCode);

        action.Filters.Add(filter);
    }
}

