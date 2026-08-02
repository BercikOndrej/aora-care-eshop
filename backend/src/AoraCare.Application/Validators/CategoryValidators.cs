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
        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(255)
            .WithMessage("Name may have max 255 characters.")
            .Matches(@"\w")
            .WithMessage("Name must include some word characters");
        RuleFor(c => c.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(1024)
            .WithMessage("Description may have max 1024 characters.")
            .Matches(@"\w")
            .WithMessage("Description must include some word characters");
    }
}

/// <summary>
///     Validates <see cref="CategoryUpdateDto"/> input.
/// </summary>
public class CategoryUpdateDtoValidator : AbstractValidator<CategoryUpdateDto>
{
    public CategoryUpdateDtoValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(255)
            .WithMessage("Name may have max 255 characters.")
            .Matches(@"\w")
            .WithMessage("Name must include some word characters")
            .When(c => c.Name is not null);
        RuleFor(c => c.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(1024)
            .WithMessage("Description may have max 1024 characters.")
            .Matches(@"\w")
            .WithMessage("Description must include some word characters")
            .When(c => c.Description is not null);
        RuleFor(c => c.SortOrder).GreaterThan(-1).When(c => c.SortOrder is not null);
    }
}
