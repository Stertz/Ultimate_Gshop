using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Itens;
using Ultimate_Gshop.Itens.Services;

namespace Ultimate_Gshop.Format.Tests;

public sealed class BulkItemEditingTests
{
    [Fact]
    public void BulkEditTargetsSelectedRecordsWithDuplicateIdsOnlyOnce()
    {
        var shop = ShopFixture.Create(3);
        shop.Itens.ForEach(item => item.ID = 42);
        var first = shop.Itens[0];
        var selected = shop.Itens[1];
        var stale = shop.Itens[2].Clone();
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();

        UpdateItems.Handle(shop.Itens, new[] { selected, selected, stale }, item => item.Amount++, history);

        Assert.Same(first, shop.Itens[0]);
        Assert.Equal(selected.Amount + 1, shop.Itens[1].Amount);
        Assert.Single(history.UndoChanges);
        var after = ShopFixture.Export(shop);
        history.Undo(shop);
        Assert.Equal(before, ShopFixture.Export(shop));
        history.Redo(shop);
        Assert.Equal(after, ShopFixture.Export(shop));
    }

    [Fact]
    public void FailedBulkValidationPreservesAllItemsAndHistory()
    {
        var shop = ShopFixture.Create(2);
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        Assert.Throws<InvalidOperationException>(() => UpdateItems.Handle(shop.Itens, shop.Itens, item =>
        {
            item.Sales[0].Price = 99;
            if (item.Place == 1) throw new InvalidOperationException("Rejected second record.");
        }, history));
        Assert.Equal(before, ShopFixture.Export(shop));
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void BulkEditsHaveIndependentSalesAndOwnersAndSkipNoOpChanges()
    {
        var shop = ShopFixture.Create(2);
        var history = new ShopTracking();
        UpdateItems.Handle(shop.Itens, shop.Itens, _ => { }, history);
        Assert.False(history.CanUndo);
        UpdateItems.Handle(shop.Itens, shop.Itens, item => item.Sales[0].Price = 1234, history);
        var after = ShopFixture.Export(shop);
        Assert.NotSame(shop.Itens[0].Sales, shop.Itens[1].Sales);
        Assert.NotSame(shop.Itens[0].Sales[0], shop.Itens[1].Sales[0]);
        Assert.NotSame(shop.Itens[0].OwnerNpcs, shop.Itens[1].OwnerNpcs);
        history.Undo(shop);
        history.Undo(shop);
        history.Redo(shop);
        history.Redo(shop);
        Assert.Equal(after, ShopFixture.Export(shop));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeleteUsesRecordIdentityAndRestoresTheActualRecord(bool detached)
    {
        var shop = ShopFixture.Create(2);
        shop.Itens[1].ID = shop.Itens[0].ID;
        var first = shop.Itens[0];
        var target = shop.Itens[1];
        var snapshot = target.Clone();
        snapshot.Name = "Stale display name";
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        Assert.True(DeleteItem.Handle(detached ? snapshot : target, shop.Itens, history));
        Assert.Same(first, Assert.Single(shop.Itens));
        history.Undo(shop);
        Assert.Equal(before, ShopFixture.Export(shop));
        history.Redo(shop);
        Assert.Single(shop.Itens);
    }

    [Fact]
    public void MissingOrAmbiguousDeleteCannotRemoveAnotherRecord()
    {
        var shop = ShopFixture.Create(1);
        var history = new ShopTracking();
        var missing = shop.Itens[0].Clone();
        missing.Place = 99;
        Assert.False(DeleteItem.Handle(missing, shop.Itens, history));
        var snapshot = shop.Itens[0].Clone();
        shop.Itens.Add(snapshot.Clone());
        Assert.Throws<InvalidOperationException>(() => DeleteItem.Handle(snapshot, shop.Itens, history));
        Assert.Equal(2, shop.Itens.Count);
        Assert.False(history.CanUndo);
    }
}
