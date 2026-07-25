using AoraCare.Domain.Common;
using FluentAssertions;

namespace AoraCare.Domain.Tests;

public class SlugHelperTests
{
    [Theory]
    [InlineData("category 1", "category-1")]
    [InlineData("Category Beauty", "category-beauty")]
    [InlineData("-category Beauty-", "category-beauty")]
    [InlineData("  category Beauty  ", "category-beauty")]
    [InlineData("##category Beauty^~", "category-beauty")]
    [InlineData("category $$#Beauty", "category-beauty")]
    [InlineData("category_Beauty", "category-beauty")]
    [InlineData("Příliš žluťoučký kůň", "prilis-zlutoucky-kun")]
    [InlineData("a__b", "a-b")]
    [InlineData("_category_", "category")]
    public void CreateSlug_WhenIncludesSpaceAndNonWordChars_BeGoodFormatedSlug(
        string name,
        string expected
    )
    {
        string slug = SlugHelper.CreateSlug(name);

        slug.Should().Be(expected);
    }
}
