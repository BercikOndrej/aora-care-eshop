using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace AoraCare.Api.Middlewares;

/// <summary>
///     Catches unhandled exceptions and converts them into a generic 500 <see cref="ProblemDetails"/> response.
/// </summary>
public class GlobalExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unhandled exception occured");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            ProblemDetails problems = new()
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Server error",
                Type = "Server error",
                Detail = "An internal server error occured",
            };

            await context.Response.WriteAsJsonAsync(problems);
        }
    }
}
