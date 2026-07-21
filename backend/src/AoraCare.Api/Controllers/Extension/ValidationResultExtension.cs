using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AoraCare.Api.Controllers.Extension;

/// <summary>
///     Copies FluentValidation errors into ASP.NET Core's model state.
/// </summary>
public static class ValidationResultExtensions
{
    /// <summary>
    ///     Adds each validation error to the model state.
    /// </summary>
    public static void AddToModelState(
        this ValidationResult result,
        ModelStateDictionary modelState
    )
    {
        foreach (var error in result.Errors)
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
    }
}
