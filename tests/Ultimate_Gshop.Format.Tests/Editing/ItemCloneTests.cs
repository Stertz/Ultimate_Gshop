using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Itens.Services;

namespace Ultimate_Gshop.Format.Tests;

public sealed class ItemCloneTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 3)]
    public void PreparedCloneHasTheNextPositionAndDoesNotModifyTheShop(int count, int expected)
    {
        var shop = ShopFixture.Create(count);
        var source = ShopFixture.CreateItem(9);
        var before = ShopFixture.Export(shop);
        var clone = CloneItems.Prepare(source, shop.Itens);
        Assert.Equal(expected, clone.Place);
        Assert.Equal(source.ID, clone.ID);
        Assert.NotSame(source.Sales[0], clone.Sales[0]);
        Assert.NotSame(source.OwnerNpcs, clone.OwnerNpcs);
        Assert.Equal(before, ShopFixture.Export(shop));
    }

    [Fact]
    public void BulkCloneKeepsDuplicateIdsUsesUniquePositionsAndSupportsHistory()
    {
        var shop = ShopFixture.Create(2);
        shop.Itens[1].ID = shop.Itens[0].ID;
        var first = shop.Itens[0];
        var second = shop.Itens[1];
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        var clones = CloneItems.Handle(shop, new[] { second, first, second, first.Clone() }, history);
        Assert.Equal(2, clones.Count);
        Assert.Equal(new[] { 2, 3 }, clones.Select(item => item.Place));
        Assert.Equal(second.Name, clones[0].Name);
        Assert.Equal(first.Name, clones[1].Name);
        Assert.Equal(4, shop.ItemAmount);
        Assert.NotSame(first.Sales[0], clones[1].Sales[0]);
        var after = ShopFixture.Export(shop);
        history.Undo(shop);
        history.Undo(shop);
        Assert.Equal(2, shop.ItemAmount);
        Assert.Equal(before, ShopFixture.Export(shop));
        history.Redo(shop);
        history.Redo(shop);
        Assert.Equal(4, shop.ItemAmount);
        Assert.Equal(after, ShopFixture.Export(shop));
    }

    [Fact]
    public void CloningTheLiveListDoesNotEnumerateNewlyInsertedClones()
    {
        var shop = ShopFixture.Create(3);
        Assert.Equal(3, CloneItems.Handle(shop, shop.Itens, new ShopTracking()).Count);
        Assert.Equal(6, shop.Itens.Count);
    }

    [Fact]
    public void StaleOrEmptySelectionPreservesRedoAndDoesNotChangeTheShop()
    {
        var shop = ShopFixture.Create(1);
        var history = new ShopTracking();
        InsertItem.Handle(shop.Itens, ShopFixture.CreateItem(1), history);
        history.Undo(shop);
        Assert.Empty(CloneItems.Handle(shop, new[] { shop.Itens[0].Clone() }, history));
        Assert.Empty(CloneItems.Handle(shop, [], history));
        Assert.True(history.CanRedo);
        Assert.Single(shop.Itens);
    }

    [Theory]
    [InlineData(int.MaxValue, 1)]
    [InlineData(int.MaxValue - 1, 2)]
    public void PositionExhaustionRejectsTheWholeCloneOperation(int lastPlace, int selectedCount)
    {
        var shop = ShopFixture.Create(2);
        shop.Itens[1].Place = lastPlace;
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        Assert.Throws<OverflowException>(() => CloneItems.Handle(shop, shop.Itens.Take(selectedCount), history));
        Assert.Equal(before, ShopFixture.Export(shop));
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void LastAvailablePositionCanBeUsedAndPrepareRejectsTheNextOne()
    {
        var shop = ShopFixture.Create(1);
        shop.Itens[0].Place = int.MaxValue - 1;
        var clone = Assert.Single(CloneItems.Handle(shop, shop.Itens, new ShopTracking()));
        Assert.Equal(int.MaxValue, clone.Place);
        Assert.Throws<OverflowException>(() => CloneItems.Prepare(clone, shop.Itens));
    }

    [Fact]
    public void ClonedDuplicateIdsAndHistorySurviveARealSessionSave()
    {
        using var directory = new TestDirectory();
        var shop = ShopFixture.Create(2);
        shop.Itens[1].ID = shop.Itens[0].ID;
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        CloneItems.Handle(shop, shop.Itens, history);
        var after = ShopFixture.Export(shop);
        SaveGshop.Handle(shop, history, new GshopSettings(), directory.Path);
        var loaded = LoadGshop.Handle(Path.Combine(directory.Path, "gshop.data"));
        var loadedHistory = ShopTracking.Load(Path.Combine(directory.Path, "tracking.json"));
        Assert.Equal(after, ShopFixture.Export(loaded));
        loadedHistory.Undo(loaded);
        loadedHistory.Undo(loaded);
        Assert.Equal(before, ShopFixture.Export(loaded));
        loadedHistory.Redo(loaded);
        loadedHistory.Redo(loaded);
        Assert.Equal(after, ShopFixture.Export(loaded));
    }
}
