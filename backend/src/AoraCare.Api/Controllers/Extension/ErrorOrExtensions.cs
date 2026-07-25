using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace AoraCare.Api.Controllers.Extension;

public static class ErrorOrExtensions
{
    public static ActionResult ToActionResult<T>(
        this ErrorOr<T> result,
        ControllerBase controller,
        int statusCode = StatusCodes.Status200OK
    ) =>
        result.Match(
            value => controller.StatusCode(statusCode, value),
            errors =>
            {
                (int httpStatusCode, string title) = errors.First().Type switch
                {
                    ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
                    ErrorType.Validation => (StatusCodes.Status400BadRequest, "Bad Request"),
                    ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
                    ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                    ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
                    _ => (StatusCodes.Status500InternalServerError, "Server Error"),
                };

                return controller.Problem(
                    statusCode: httpStatusCode,
                    title: title,
                    detail: errors.First().Description
                );
            }
        );
}
