using AoraCare.Application.Dtos;
using FluentValidation;

namespace AoraCare.Application.Validators;

public class ProductAddDtoValidator : AbstractValidator<ProductAddDto>
{
    public ProductAddDtoValidator()
    {
        RuleFor(p => p.Name)
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

public class ProductUpdateDtoValidator : AbstractValidator<ProductUpdateDto>
{
    public ProductUpdateDtoValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(255)
            .WithMessage("Name may have max 255 characters.")
            .Matches(@"\w")
            .WithMessage("Name must include some word characters")
            .When(p => p.Name is not null);
        RuleFor(p => p.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(1024)
            .WithMessage("Description may have max 1024 characters.")
            .Matches(@"\w")
            .WithMessage("Description must include some word characters")
            .When(p => p.Description is not null);
        RuleFor(p => p.SortOrder).GreaterThan(-1).When(p => p.SortOrder is not null);
    }
}
