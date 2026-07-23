using AoraCare.Application.Services;
using AoraCare.Application.Services.Interfaces;
using AoraCare.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AoraCare.Application.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Services
        services.AddScoped<ICategoryService, CategoryService>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<CategoryAddDtoValidator>();

        return services;
    }
}
