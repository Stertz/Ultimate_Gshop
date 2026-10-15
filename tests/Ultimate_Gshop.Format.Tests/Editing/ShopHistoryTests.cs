using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Itens.Services;

namespace Ultimate_Gshop.Format.Tests;

public sealed class ShopHistoryTests
{
    [Fact]
    public void EmptyHistoryIsANoOp()
    {
        var shop = ShopFixture.Create();
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        history.Undo(shop);
        history.Redo(shop);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Equal(before, ShopFixture.Export(shop));
    }

    [Fact]
    public void InsertCanBeUndoneAndRedoneWithAnIndependentBackup()
    {
        var shop = ShopFixture.Create(1);
        var history = new ShopTracking();
        var item = ShopFixture.CreateItem(1);
        InsertItem.Handle(shop.Itens, item, history);

        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
        history.Undo(shop);
        Assert.Single(shop.Itens);
        Assert.True(history.CanRedo);
        history.Redo(shop);
        Assert.Equal(2, shop.Itens.Count);
        Assert.NotSame(item, shop.Itens[1]);
        Assert.Equal(item.ID, shop.Itens[1].ID);
    }

    [Fact]
    public void UpdateUndoAndRedoRestoreTheExpectedValues()
    {
        var shop = ShopFixture.Create(1);
        var history = new ShopTracking();
        var original = shop.Itens[0].Clone();
        var changed = original.Clone();
        changed.Name = "Edited";
        changed.Sales[0].Price = 5678;
        UpdateItem.Handle(shop.Itens, changed, original, history);

        Assert.Equal("Edited", shop.Itens[0].Name);
        history.Undo(shop);
        Assert.Equal(original.Name, shop.Itens[0].Name);
        Assert.Equal(original.Sales[0].Price, shop.Itens[0].Sales[0].Price);
        history.Redo(shop);
        Assert.Equal("Edited", shop.Itens[0].Name);
        Assert.Equal(5678, shop.Itens[0].Sales[0].Price);
    }

    [Fact]
    public void DeleteUndoAndRedoKeepTheOriginalIndex()
    {
        var shop = ShopFixture.Create(2);
        var history = new ShopTracking();
        var removed = shop.Itens[0];
        shop.Itens.RemoveAt(0);
        history.ItemDeleted(0, removed);
        history.Undo(shop);
        Assert.Equal(removed.ID, shop.Itens[0].ID);
        history.Redo(shop);
        Assert.Equal(1001, Assert.Single(shop.Itens).ID);
    }

    [Fact]
    public void NewEditInvalidatesRedoAndNotifiesBothHistoryButtons()
    {
        var shop = ShopFixture.Create(1);
        var history = new ShopTracking();
        var notifications = new List<string?>();
        history.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        InsertItem.Handle(shop.Itens, ShopFixture.CreateItem(1), history);
        history.Undo(shop);
        Assert.True(history.CanRedo);
        notifications.Clear();

        InsertItem.Handle(shop.Itens, ShopFixture.CreateItem(2), history);

        Assert.False(history.CanRedo);
        Assert.Contains(nameof(ShopTracking.CanUndo), notifications);
        Assert.Contains(nameof(ShopTracking.CanRedo), notifications);
    }
}
