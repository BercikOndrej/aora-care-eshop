using AoraCare.Api.Middlewares;
using Serilog;

namespace AoraCare.Api.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Logging with Serilog
        services.AddSerilog(
            (services, lc) =>
                lc
                    .ReadFrom.Configuration(configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
        );

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
