using Ultimate_Gshop.Itens;
using Ultimate_Gshop.Itens.Services;

namespace Ultimate_Gshop.Format.Tests;

public sealed class ItemSearchTests
{
    [Fact]
    public void BroadeningOrReplacingAQueryRestoresItemsFromTheShop()
    {
        var items = new List<Item>
        {
            new() { Name = "Red dragon" }, new() { Name = "Blue dragon" }, new() { Name = "Wolf" }
        };
        Assert.Same(items[0], Assert.Single(FilterItems.Handle(items, -1, -1, "Red dragon")));
        Assert.Equal(2, FilterItems.Handle(items, -1, -1, "dragon").Count);
        Assert.Same(items[2], Assert.Single(FilterItems.Handle(items, -1, -1, "wolf")));
        Assert.Empty(FilterItems.Handle(items, -1, -1, "missing"));
        Assert.Equal(items, FilterItems.Handle(items, -1, -1, ""));
    }

    [Theory]
    [InlineData(null, 3)]
    [InlineData("   ", 3)]
    [InlineData(" DRAGON   red ", 1)]
    [InlineData("234", 2)]
    [InlineData("234x", 0)]
    [InlineData("中文", 1)]
    public void SearchPreservesNameTermsUnicodeAndPartialNumericIds(string? query, int count)
    {
        var items = new[]
        {
            new Item { ID = 12345, Name = "Red Dragon" },
            new Item { ID = 234, Name = "中文" },
            new Item { ID = 9, Name = null! }
        };
        Assert.Equal(count, FilterItems.Handle(items, -1, -1, query).Count);
    }

    [Theory]
    [InlineData(-1, 0, 4)]
    [InlineData(0, -1, 2)]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(9, -1, 0)]
    public void SearchRespectsCategoryScope(int category, int subCategory, int count)
    {
        var items = Enumerable.Range(0, 4).Select(index => new Item
        {
            ID = 42, Name = "Dragon", CategoryIndex = index / 2, SubCategoryIndex = index % 2
        }).ToList();
        Assert.Equal(count, FilterItems.Handle(items, category, subCategory, "dragon").Count);
    }
}
