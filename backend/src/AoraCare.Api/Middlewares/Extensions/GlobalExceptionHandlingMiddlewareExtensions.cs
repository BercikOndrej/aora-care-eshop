namespace AoraCare.Api.Middlewares;

/// <summary>
///     Registers <see cref="GlobalExceptionHandlingMiddleware"/> in the request pipeline.
/// </summary>
public static class GlobalExceptionHandlingMiddlewareExtensions
{
    /// <summary>
    ///     Adds the global exception handling middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder app) =>
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
}
