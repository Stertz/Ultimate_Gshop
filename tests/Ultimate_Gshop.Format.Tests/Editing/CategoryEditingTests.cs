using System.Text.Json;
using Ultimate_Gshop.Categories.Services;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Itens.Services;

namespace Ultimate_Gshop.Format.Tests;

public sealed class CategoryEditingTests
{
    private static Shop Create()
    {
        var shop = ShopFixture.Create(4);
        shop.Categories[0].SubCategories = ["First", "Empty", "Last"];
        shop.Itens[0].CategoryIndex = 0;
        shop.Itens[0].SubCategoryIndex = 0;
        shop.Itens[2].CategoryIndex = 0;
        shop.Itens[2].SubCategoryIndex = 2;
        shop.Itens.ForEach(item => item.ID = 42);
        return shop;
    }

    [Theory]
    [InlineData(null, 2)]
    [InlineData(0, 1)]
    [InlineData(2, 1)]
    public void ResetTargetsTheRequestedScopeAndUndoesAsOneOperation(int? subCategory, int removed)
    {
        var shop = Create();
        var list = shop.Itens;
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        Assert.Equal(removed, EditCategories.Reset(shop, 0, subCategory, history));
        Assert.Same(list, shop.Itens);
        Assert.Equal(4 - removed, shop.ItemAmount);
        Assert.Single(history.UndoChanges);
        Assert.DoesNotContain(shop.Itens, item => item.CategoryIndex == 0 &&
            (subCategory is null || item.SubCategoryIndex == subCategory));
        var after = ShopFixture.Export(shop);
        history.Undo(shop);
        Assert.Equal(4, shop.ItemAmount);
        Assert.Equal(before, ShopFixture.Export(shop));
        history.Redo(shop);
        Assert.Equal(after, ShopFixture.Export(shop));
    }

    [Fact]
    public void EmptyResetPreservesRedoAndDoesNotRecordHistory()
    {
        var shop = Create();
        var history = new ShopTracking();
        InsertItem.Handle(shop.Itens, ShopFixture.CreateItem(5), history);
        history.Undo(shop);
        Assert.Equal(0, EditCategories.Reset(shop, 0, 1, history));
        Assert.Equal(0, EditCategories.Reset(shop, 7, null, history));
        Assert.Empty(history.UndoChanges);
        Assert.True(history.CanRedo);
    }

    [Fact]
    public void ResetAllItemsSupportsAnEmptyInverseSnapshot()
    {
        var shop = ShopFixture.Create(2);
        shop.Itens.ForEach(item => item.CategoryIndex = 0);
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        EditCategories.Reset(shop, 0, null, history);
        Assert.Empty(shop.Itens);
        history.Undo(shop);
        Assert.Equal(before, ShopFixture.Export(shop));
        history.Redo(shop);
        Assert.Empty(shop.Itens);
        Assert.Equal(0, shop.ItemAmount);
    }

    [Fact]
    public void DeletingEmptySubcategoryShiftsFollowingIndicesAndRestoresMetadata()
    {
        var shop = Create();
        var hidden = shop.Itens[1];
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        Assert.True(EditCategories.DeleteSubCategory(shop, 0, 1, history));
        Assert.Equal(new[] { "First", "Last" }, shop.Categories[0].SubCategories);
        Assert.Equal(1, shop.Itens[2].SubCategoryIndex);
        Assert.Same(hidden, shop.Itens[1]);
        Assert.Single(history.UndoChanges);
        var after = ShopFixture.Export(shop);
        history.Undo(shop);
        Assert.Equal(before, ShopFixture.Export(shop));
        history.Redo(shop);
        Assert.Equal(after, ShopFixture.Export(shop));
    }

    [Fact]
    public void NonemptySubcategoryCannotBeDeletedOrInvalidateRedo()
    {
        var shop = Create();
        var history = new ShopTracking();
        EditCategories.DeleteSubCategory(shop, 0, 1, history);
        history.Undo(shop);
        var before = ShopFixture.Export(shop);
        Assert.False(EditCategories.DeleteSubCategory(shop, 0, 0, history));
        Assert.True(history.CanRedo);
        Assert.Equal(before, ShopFixture.Export(shop));
    }

    [Fact]
    public void DeletingTheLastEmptySubcategoryHasNoAffectedItemBackups()
    {
        var shop = Create();
        shop.Categories[0].SubCategories.Add("Unused tail");
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        Assert.True(EditCategories.DeleteSubCategory(shop, 0, 3, history));
        Assert.Empty(Assert.Single(history.UndoChanges).ItemsBackup!);
        history.Undo(shop);
        Assert.Equal(before, ShopFixture.Export(shop));
    }

    [Theory]
    [InlineData(-1, null)]
    [InlineData(8, null)]
    [InlineData(0, -1)]
    [InlineData(0, 3)]
    public void InvalidResetScopePreservesDataAndHistory(int category, int? subCategory)
    {
        var shop = Create();
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        Assert.Throws<ArgumentOutOfRangeException>(() => EditCategories.Reset(shop, category, subCategory, history));
        Assert.Equal(before, ShopFixture.Export(shop));
        Assert.False(history.CanUndo);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(8, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 3)]
    public void InvalidDeleteScopePreservesDataAndHistory(int category, int subCategory)
    {
        var shop = Create();
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        Assert.Throws<ArgumentOutOfRangeException>(() => EditCategories.DeleteSubCategory(shop, category, subCategory, history));
        Assert.Equal(before, ShopFixture.Export(shop));
        Assert.False(history.CanUndo);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SavedCategoryOperationsCanBeUndoneAfterReload(bool reset)
    {
        using var directory = new TestDirectory();
        var shop = Create();
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        if (reset) EditCategories.Reset(shop, 0, null, history);
        else EditCategories.DeleteSubCategory(shop, 0, 1, history);
        SaveGshop.Handle(shop, history, new GshopSettings(), directory.Path);
        var loaded = LoadGshop.Handle(Path.Combine(directory.Path, "gshop.data"));
        var loadedHistory = ShopTracking.Load(Path.Combine(directory.Path, "tracking.json"));
        var afterReload = ShopFixture.Export(loaded);
        loadedHistory.Undo(loaded);
        Assert.Equal(before, ShopFixture.Export(loaded));
        loadedHistory = JsonSerializer.Deserialize<ShopTracking>(JsonSerializer.Serialize(loadedHistory))!;
        loadedHistory.Redo(loaded);
        Assert.Equal(afterReload, ShopFixture.Export(loaded));
    }

    [Fact]
    public void ResetRetainsPriorIndexBasedHistory()
    {
        var shop = Create();
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        var original = shop.Itens[2];
        var updated = original.Clone();
        updated.Amount = 999;
        UpdateItem.Handle(shop.Itens, updated, original, history);
        EditCategories.Reset(shop, 0, null, history);
        history.Undo(shop);
        history.Undo(shop);
        Assert.Equal(before, ShopFixture.Export(shop));
        history.Redo(shop);
        history.Redo(shop);
        Assert.Equal(2, shop.Itens.Count);
    }

    [Theory]
    [InlineData(EChangeAction.ResetItems)]
    [InlineData(EChangeAction.Category)]
    public void MissingHistoryPayloadDoesNotChangeItemsOrLoseTheEntry(EChangeAction action)
    {
        var shop = Create();
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        history.UndoChanges.Push(new ShopChange { Action = action });
        Assert.Throws<InvalidDataException>(() => history.Undo(shop));
        Assert.Single(history.UndoChanges);
        Assert.Equal(before, ShopFixture.Export(shop));
    }

    [Fact]
    public void RenameAddAndDeleteKeepCategoryHistoryInOrderAfterReload()
    {
        var shop = Create();
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        Assert.True(EditCategories.RenameCategory(shop, 0, "Renamed category", history));
        Assert.True(EditCategories.AddSubCategory(shop, 0, "Tail", history));
        Assert.True(EditCategories.RenameSubCategory(shop, 0, 3, " Final ", history));
        Assert.True(EditCategories.DeleteSubCategory(shop, 0, 1, history));
        var after = ShopFixture.Export(shop);
        history = JsonSerializer.Deserialize<ShopTracking>(JsonSerializer.Serialize(history))!;
        for (var index = 0; index < 4; index++) history.Undo(shop);
        Assert.Equal(before, ShopFixture.Export(shop));
        for (var index = 0; index < 4; index++) history.Redo(shop);
        Assert.Equal(after, ShopFixture.Export(shop));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankMetadataChangesDoNotModifyDataOrInvalidateRedo(string? title)
    {
        var shop = Create();
        var history = new ShopTracking();
        EditCategories.DeleteSubCategory(shop, 0, 1, history);
        history.Undo(shop);
        var before = ShopFixture.Export(shop);
        Assert.False(EditCategories.RenameCategory(shop, 0, title, history));
        Assert.False(EditCategories.RenameSubCategory(shop, 0, 0, title, history));
        Assert.False(EditCategories.AddSubCategory(shop, 0, title, history));
        Assert.True(history.CanRedo);
        Assert.Equal(before, ShopFixture.Export(shop));
    }

    [Fact]
    public void NoOpAndDuplicateRenameLeaveHistoryUnchangedButCaseOnlyRenameIsAllowed()
    {
        var shop = Create();
        var history = new ShopTracking();
        Assert.False(EditCategories.RenameCategory(shop, 0, shop.Categories[0].Title, history));
        Assert.False(EditCategories.RenameSubCategory(shop, 0, 0, "First", history));
        Assert.False(EditCategories.RenameSubCategory(shop, 0, 0, "LAST", history));
        Assert.False(history.CanUndo);
        Assert.True(EditCategories.RenameSubCategory(shop, 0, 0, "FIRST", history));
        Assert.Equal("FIRST", shop.Categories[0].SubCategories[0]);
        history.Undo(shop);
        Assert.Equal("First", shop.Categories[0].SubCategories[0]);
    }

    [Fact]
    public void SubcategoryLimitIsCheckedBeforeRecordingHistory()
    {
        var shop = Create();
        shop.Categories[0].SubCategories = Enumerable.Range(0, 10).Select(index => $"Sub {index}").ToList();
        var history = new ShopTracking();
        Assert.True(EditCategories.AddSubCategory(shop, 0, "Eleventh", history));
        Assert.False(EditCategories.AddSubCategory(shop, 0, "Twelfth", history));
        Assert.Single(history.UndoChanges);
        Assert.Equal(11, shop.Categories[0].SubCategories.Count);
        history.Undo(shop);
        Assert.Equal(10, shop.Categories[0].SubCategories.Count);
    }
}
