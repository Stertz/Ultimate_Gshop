using Ultimate_Gshop.Categories;

namespace Ultimate_Gshop.Format.Tests;

public sealed class CategoryValidationTests
{
    [Fact]
    public void RejectsMissingCategory()
    {
        Assert.Throws<ArgumentNullException>(() => CategoryValidator.Validate(null!));
    }

    [Fact]
    public void AcceptsTheExistingTitleAndSubcategoryLimits()
    {
        var category = new Category
        {
            Title = new string('A', 32),
            SubCategories = Enumerable.Range(0, 11).Select(index => $"Subcategory {index}").ToList()
        };

        CategoryValidator.Validate(category);
    }

    [Fact]
    public void RejectsATitleBeyondTheExistingLimit()
    {
        var category = new Category { Title = new string('A', 33) };
        Assert.Throws<InvalidOperationException>(() => CategoryValidator.Validate(category));
    }

    [Fact]
    public void RejectsTooManySubcategories()
    {
        var category = new Category
        {
            Title = "Shop",
            SubCategories = Enumerable.Range(0, 12).Select(index => $"Subcategory {index}").ToList()
        };
        Assert.Throws<InvalidOperationException>(() => CategoryValidator.Validate(category));
    }
}
