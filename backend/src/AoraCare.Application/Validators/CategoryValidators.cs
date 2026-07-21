using AoraCare.Application.Dtos;
using FluentValidation;

namespace AoraCare.Application.Validators;

/// <summary>
///     Validates <see cref="CategoryAddDto"/> input.
/// </summary>
public class CategoryAddDtoValidator : AbstractValidator<CategoryAddDto>
{
    public CategoryAddDtoValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Description).NotEmpty().MaximumLength(1024);
    }
}

/// <summary>
///     Validates <see cref="CategoryUpdateDto"/> input.
/// </summary>
public class CategoryUpdateDtoValidator : AbstractValidator<CategoryUpdateDto>
{
    public CategoryUpdateDtoValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(255).When(c => c.Name is not null);
        RuleFor(c => c.Description)
            .NotEmpty()
            .MaximumLength(1024)
            .When(c => c.Description is not null);
        RuleFor(c => c.SortOrder).GreaterThan(-1).When(c => c.SortOrder is not null);
    }
}
