using AoraCare.Application.Dtos;
using AoraCare.Application.Validators;
using FluentAssertions;

namespace AoraCare.Application.Validators.Tests;

[Trait("Category", "Unit")]
public class CategoryValidatorsTests
{
    private const string ValidName = "Valid Category Name";
    private const string ValidDescription = "Valid category description.";

    [Fact]
    public void CategoryAddDtoValidator_WhenNameAtMaxLength_IsValid()
    {
        string name = new string('a', 255);
        CategoryAddDto dto = new CategoryAddDto(name, ValidDescription, true);
        CategoryAddDtoValidator validator = new CategoryAddDtoValidator();

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CategoryAddDtoValidator_WhenDescriptionAtMaxLength_IsValid()
    {
        string description = new string('B', 1024);
        CategoryAddDto dto = new CategoryAddDto(ValidName, description, true);
        CategoryAddDtoValidator validator = new CategoryAddDtoValidator();

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CategoryAddDtoValidator_WhenNameExceedsMaxLength_IsInvalid()
    {
        string name = new string('a', 256);
        CategoryAddDto dto = new CategoryAddDto(name, ValidDescription, true);
        CategoryAddDtoValidator validator = new CategoryAddDtoValidator();

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CategoryAddDto.Name));
    }

    [Fact]
    public void CategoryAddDtoValidator_WhenDescriptionExceedsMaxLength_IsInvalid()
    {
        string description = new string('B', 1025);
        CategoryAddDto dto = new CategoryAddDto(ValidName, description, true);
        CategoryAddDtoValidator validator = new CategoryAddDtoValidator();

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CategoryAddDto.Description));
    }

    [Fact]
    public void CategoryAddDtoValidator_WhenNameIsEmpty_IsInvalid()
    {
        CategoryAddDto dto = new CategoryAddDto(string.Empty, ValidDescription, true);
        CategoryAddDtoValidator validator = new CategoryAddDtoValidator();

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CategoryAddDto.Name));
    }

    [Fact]
    public void CategoryAddDtoValidator_WhenDescriptionIsEmpty_IsInvalid()
    {
        CategoryAddDto dto = new CategoryAddDto(ValidName, string.Empty, true);
        CategoryAddDtoValidator validator = new CategoryAddDtoValidator();

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CategoryAddDto.Description));
    }

    [Fact]
    public void CategoryAddDtoValidator_WhenNameIsWhitespaceOnly_IsInvalid()
    {
        CategoryAddDto dto = new CategoryAddDto("   ", ValidDescription, true);
        CategoryAddDtoValidator validator = new CategoryAddDtoValidator();

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CategoryAddDto.Name));
    }

    [Fact]
    public void CategoryAddDtoValidator_WhenDescriptionIsWhitespaceOnly_IsInvalid()
    {
        CategoryAddDto dto = new CategoryAddDto(ValidName, "   ", true);
        CategoryAddDtoValidator validator = new CategoryAddDtoValidator();

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CategoryAddDto.Description));
    }
}
