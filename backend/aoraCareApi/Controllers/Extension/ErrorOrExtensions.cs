using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace aoraCareApi.Controllers.Extension;

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
                controller.Problem(
                    statusCode: errors.First().Type switch
                    {
                        ErrorType.NotFound => StatusCodes.Status404NotFound,
                        ErrorType.Validation => StatusCodes.Status400BadRequest,
                        ErrorType.Conflict => StatusCodes.Status409Conflict,
                        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                        _ => StatusCodes.Status500InternalServerError,
                    },
                    title: errors.First().Description
                )
        );
}
