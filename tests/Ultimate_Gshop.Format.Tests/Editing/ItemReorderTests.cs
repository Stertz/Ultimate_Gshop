using System.Text.Json;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Itens;
using Ultimate_Gshop.Itens.Services;

namespace Ultimate_Gshop.Format.Tests;

public sealed class ItemReorderTests
{
    [Theory]
    [InlineData(0, 3, 1001, 1002, 1000)]
    [InlineData(2, 0, 1002, 1000, 1001)]
    [InlineData(0, 2, 1001, 1000, 1002)]
    [InlineData(1, -20, 1001, 1000, 1002)]
    [InlineData(0, 99, 1001, 1002, 1000)]
    public void MoveUsesInsertionBoundariesAndOneUndoEntry(int source, int target, int first, int second, int third)
    {
        var shop = ShopFixture.Create(3);
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        var moving = shop.Itens[source];

        Assert.Same(moving, ReorderItems.Handle(shop.Itens, shop.Itens, source, target, history));
        Assert.Equal(new[] { first, second, third }, shop.Itens.Select(item => item.ID));
        Assert.Equal(new[] { 0, 1, 2 }, shop.Itens.Select(item => item.Place));
        Assert.Single(history.UndoChanges);
        var after = ShopFixture.Export(shop);
        history.Undo(shop);
        Assert.Equal(before, ShopFixture.Export(shop));
        history.Redo(shop);
        Assert.Equal(after, ShopFixture.Export(shop));
    }

    [Fact]
    public void FilteredReorderPreservesHiddenRecordsAndSparseSlotsWithDuplicateIds()
    {
        var shop = ShopFixture.Create(4);
        shop.Itens.ForEach(item => item.ID = 42);
        var hidden = shop.Itens[1];
        var last = shop.Itens[3];
        var moving = shop.Itens[0];
        var history = new ShopTracking();
        var before = ShopFixture.Export(shop);

        ReorderItems.Handle(shop.Itens, new[] { moving, shop.Itens[2] }, 0, 2, history);

        Assert.Same(moving, shop.Itens[2]);
        Assert.Same(hidden, shop.Itens[1]);
        Assert.Same(last, shop.Itens[3]);
        Assert.Equal(new[] { 0, 1, 2, 3 }, shop.Itens.Select(item => item.Place));
        history.Undo(shop);
        Assert.Equal(before, ShopFixture.Export(shop));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(99, 0)]
    public void MissingSourceOrUnchangedOrderPreservesRedo(int source, int target)
    {
        var shop = ShopFixture.Create(3);
        var history = new ShopTracking();
        InsertItem.Handle(shop.Itens, ShopFixture.CreateItem(3), history);
        history.Undo(shop);
        var before = ShopFixture.Export(shop);
        ReorderItems.Handle(shop.Itens, shop.Itens, source, target, history);
        Assert.Equal(before, ShopFixture.Export(shop));
        Assert.Empty(history.UndoChanges);
        Assert.True(history.CanRedo);
    }

    [Fact]
    public void ActualMoveInvalidatesRedoAndNotifiesBothButtons()
    {
        var shop = ShopFixture.Create(3);
        var history = new ShopTracking();
        InsertItem.Handle(shop.Itens, ShopFixture.CreateItem(3), history);
        history.Undo(shop);
        var notifications = new List<string?>();
        history.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        ReorderItems.Handle(shop.Itens, shop.Itens, 0, 3, history);

        Assert.False(history.CanRedo);
        Assert.Contains(nameof(ShopTracking.CanUndo), notifications);
        Assert.Contains(nameof(ShopTracking.CanRedo), notifications);
    }

    [Fact]
    public void SaveAndReloadRetainTheOrderAndBothHistoryDirections()
    {
        using var directory = new TestDirectory();
        var shop = ShopFixture.Create(3);
        var history = new ShopTracking();
        var before = ShopFixture.Export(shop);
        ReorderItems.Handle(shop.Itens, shop.Itens, 0, 3, history);
        var after = ShopFixture.Export(shop);
        SaveGshop.Handle(shop, history, new GshopSettings(), directory.Path);
        var loaded = LoadGshop.Handle(Path.Combine(directory.Path, "gshop.data"));
        var loadedHistory = ShopTracking.Load(Path.Combine(directory.Path, "tracking.json"));
        Assert.Equal(after, ShopFixture.Export(loaded));

        loadedHistory.Undo(loaded);
        Assert.Equal(before, ShopFixture.Export(loaded));
        loadedHistory = JsonSerializer.Deserialize<ShopTracking>(JsonSerializer.Serialize(loadedHistory))!;
        loadedHistory.Redo(loaded);
        Assert.Equal(after, ShopFixture.Export(loaded));
    }

    [Fact]
    public void ReorderAndIndividualEditsCanBeUndoneInSequence()
    {
        var shop = ShopFixture.Create(3);
        var history = new ShopTracking();
        var before = ShopFixture.Export(shop);
        ReorderItems.Handle(shop.Itens, shop.Itens, 0, 3, history);
        var original = shop.Itens[2];
        var changed = original.Clone();
        changed.Name = "Edited after moving";
        UpdateItem.Handle(shop.Itens, changed, original, history);
        var after = ShopFixture.Export(shop);
        history.Undo(shop);
        history.Undo(shop);
        Assert.Equal(before, ShopFixture.Export(shop));
        history.Redo(shop);
        history.Redo(shop);
        Assert.Equal(after, ShopFixture.Export(shop));
    }

    [Fact]
    public void AmbiguousPositionsAreRejectedWithoutMutatingTheShop()
    {
        var shop = ShopFixture.Create(2);
        shop.Itens[1].Place = 0;
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        Assert.Throws<InvalidOperationException>(() => ReorderItems.Handle(shop.Itens, shop.Itens, 0, 2, history));
        Assert.Equal(before, ShopFixture.Export(shop));
        Assert.False(history.CanUndo);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidPersistedReorderDoesNotLoseHistoryOrPartiallyChangeItems(bool redo)
    {
        var shop = ShopFixture.Create(1);
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        var stack = redo ? history.RedoChanges : history.UndoChanges;
        stack.Push(new ShopChange
        {
            Action = EChangeAction.Reorder,
            ItemsBackup = new() { [0] = ShopFixture.CreateItem(4), [99] = ShopFixture.CreateItem(5) }
        });
        Assert.Throws<InvalidDataException>(() => { if (redo) history.Redo(shop); else history.Undo(shop); });
        Assert.Single(stack);
        Assert.Equal(before, ShopFixture.Export(shop));
    }
}
