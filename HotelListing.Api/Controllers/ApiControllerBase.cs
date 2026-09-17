using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace HotelListing.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<T> ToActionResult<T>(Result<T> result) =>
        result.IsSuccess
            ? Ok(result.Value)
            : MapErrorsToResponse(result.Errors);

    protected ActionResult ToActionResult(Result result) =>
        result.IsSuccess
            ? NoContent()
            : MapErrorsToResponse(result.Errors);

    protected ActionResult MapErrorsToResponse(Error[] errors)
    {
        if (errors is null || errors.Length == 0)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An error occurred",
                detail: "No error details provided"
            );
        }

        var error = errors.FirstOrDefault();
        var errorDetails = string.Join("; ", errors.Select(e => e.Description));

        return error.Code switch
        {
            ErrorCodes.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found",
                detail: errorDetails
            ),
            ErrorCodes.Validation => ValidationProblem(
                title: "Validation failed",
                detail: errorDetails
            ),
            ErrorCodes.BadRequest => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "BadRequest",
                detail: errorDetails
            ),
            ErrorCodes.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: ErrorCodes.Conflict,
                detail: errorDetails
            ),
            ErrorCodes.Forbid => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: errorDetails
            ),
            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: error.Code,
                detail: errorDetails
            )
        };
    }

    protected ActionResult<T> ToCreatedAtActionResult<T>(
        Result<T> result,
        string actionName,
        Func<T, object> routeValues) =>
        result.IsSuccess
            ? CreatedAtAction(actionName, routeValues(result.Value!), result.Value)
            : MapErrorsToResponse(result.Errors);
}