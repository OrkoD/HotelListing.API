using HotelListing.Api.Results;
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
            return Problem();

        var error = errors.FirstOrDefault();

        return error.Code switch
        {
            "NotFound" => NotFound(error.Description),        // 404
            "BadRequest" => BadRequest(error.Description),    // 400
            "Validation" => BadRequest(error.Description),    // 400
            "Conflict" => Conflict(error.Description),        // 409
            _ => Problem(detail: string.Join("; ", errors.Select(e => e.Description)), title: error.Code)
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