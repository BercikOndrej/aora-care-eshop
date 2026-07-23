using AoraCare.Api.Middlewares;
using AoraCare.Application.Configuration;
using AoraCare.Infrastructure.Configuration;

namespace AoraCare.Api.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        // Middlewares registartion
        services.AddTransient<GlobalExceptionHandlingMiddleware>();

        // Controllers
        services.AddControllers(options => options.SuppressAsyncSuffixInActionNames = false);

        // Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        return services;
    }
}
