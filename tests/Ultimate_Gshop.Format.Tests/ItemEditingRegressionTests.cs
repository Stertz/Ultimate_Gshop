using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Itens;
using Ultimate_Gshop.Itens.Services;

namespace Ultimate_Gshop.Format.Tests;

public sealed class ItemEditingRegressionTests
{
    [Fact]
    public void AnUnchangedDeepCloneDoesNotCreateAnUndoEntry()
    {
        var original = ShopFixture.CreateItem();
        var items = new List<Item> { original };
        var history = new ShopTracking();

        var result = UpdateItem.Handle(items, original.Clone(), original, history);

        Assert.Same(original, result);
        Assert.Same(original, items[0]);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void ChangingTheGameIdUpdatesTheOriginalRecord()
    {
        var original = ShopFixture.CreateItem();
        var items = new List<Item> { original };
        var changed = original.Clone();
        changed.ID = 2000;
        var history = new ShopTracking();

        UpdateItem.Handle(items, changed, original, history);

        Assert.Equal(2000, Assert.Single(items).ID);
        Assert.True(history.CanUndo);
    }

    [Fact]
    public void DuplicateGameIdsDoNotRedirectAnEditToTheFirstRecord()
    {
        var first = ShopFixture.CreateItem(0);
        var second = ShopFixture.CreateItem(1);
        second.ID = first.ID;
        var items = new List<Item> { first, second };
        var changed = second.Clone();
        changed.Name = "Edited second record";

        UpdateItem.Handle(items, changed, second, new ShopTracking());

        Assert.Same(first, items[0]);
        Assert.Equal(first.Name, items[0].Name);
        Assert.Equal("Edited second record", items[1].Name);
    }

    [Fact]
    public void MissingOriginalRecordCannotReplaceAnotherItemWithTheSameGameId()
    {
        var actual = ShopFixture.CreateItem(0);
        var missing = ShopFixture.CreateItem(1);
        missing.ID = actual.ID;
        var changed = missing.Clone();
        changed.Name = "Missing record";
        var history = new ShopTracking();
        var items = new List<Item> { actual };

        Assert.Throws<InvalidOperationException>(() => UpdateItem.Handle(items, changed, missing, history));

        Assert.Same(actual, Assert.Single(items));
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void AmbiguousDetachedOriginalIsRejectedWithoutChangingHistory()
    {
        var first = ShopFixture.CreateItem();
        var second = first.Clone();
        var detached = first.Clone();
        var changed = detached.Clone();
        changed.Amount++;
        var items = new List<Item> { first, second };
        var history = new ShopTracking();

        Assert.Throws<InvalidOperationException>(() => UpdateItem.Handle(items, changed, detached, history));

        Assert.Same(first, items[0]);
        Assert.Same(second, items[1]);
        Assert.False(history.CanUndo);
    }

    [Theory]
    [InlineData(nameof(Item.ID))]
    [InlineData(nameof(Item.Amount))]
    [InlineData(nameof(Item.Place))]
    [InlineData(nameof(Item.CategoryIndex))]
    [InlineData(nameof(Item.SubCategoryIndex))]
    [InlineData(nameof(Item.GiftID))]
    [InlineData(nameof(Item.GiftAmount))]
    [InlineData(nameof(Item.GiftTime))]
    [InlineData(nameof(Item.ILogPrice))]
    [InlineData(nameof(Item.LimitPeriod))]
    [InlineData(nameof(Item.AvailFrequency))]
    [InlineData(nameof(Item.Class))]
    [InlineData(nameof(Item.Reserve1))]
    [InlineData(nameof(Item.Reserve2))]
    [InlineData(nameof(Item.Reserve3))]
    [InlineData(nameof(Item.Reserve4))]
    [InlineData(nameof(Item.Reserve5))]
    [InlineData(nameof(Item.Reserve6))]
    public void ComparisonDetectsEachIntegerFieldIndependently(string propertyName)
    {
        var original = ShopFixture.CreateItem();
        var changed = original.Clone();
        changed.Sales = original.Sales;
        var property = typeof(Item).GetProperty(propertyName)!;
        property.SetValue(changed, (int)property.GetValue(original)! + 1);

        Assert.False(ItemComparer.Handle(original, changed));
    }

    [Theory]
    [InlineData(nameof(Sale.Price))]
    [InlineData(nameof(Sale.SellingEndTime))]
    [InlineData(nameof(Sale.During))]
    [InlineData(nameof(Sale.SellingStartTime))]
    [InlineData(nameof(Sale.Control))]
    [InlineData(nameof(Sale.Day))]
    [InlineData(nameof(Sale.Status))]
    [InlineData(nameof(Sale.Flags))]
    [InlineData(nameof(Sale.VipLevelRequired))]
    public void ComparisonDetectsEachSaleField(string propertyName)
    {
        var original = ShopFixture.CreateItem();
        var changed = original.Clone();
        var property = typeof(Sale).GetProperty(propertyName)!;
        property.SetValue(changed.Sales[0], (int)property.GetValue(original.Sales[0])! + 1);

        Assert.False(ItemComparer.Handle(original, changed));
    }

    [Fact]
    public void ComparisonDetectsSaleCountAndOwnerChanges()
    {
        var original = ShopFixture.CreateItem();
        var changed = original.Clone();
        changed.Sales.RemoveAt(3);
        Assert.False(ItemComparer.Handle(original, changed));
        changed = original.Clone();
        changed.OwnerNpcs[0]++;
        Assert.False(ItemComparer.Handle(original, changed));
        Assert.False(ItemComparer.Handle(null, original));
        Assert.False(ItemComparer.Handle(original, null));
    }
}
