using System.Text.Json;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Itens.Services;
using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Format.Tests;

public sealed class SessionSnapshotTests
{
    [Fact]
    public async Task SnapshotOwnsAllNestedDataAndPreservesBothStackOrders()
    {
        using var directory = new TestDirectory();
        var shop = ShopFixture.Create(3);
        var tracking = new ShopTracking();
        tracking.ItemUpdated(0, shop.Itens[0]);
        tracking.ItemUpdated(1, shop.Itens[1]);
        tracking.Undo(shop);
        tracking.UndoChanges.Push(new ShopChange
        {
            Action = EChangeAction.Category,
            CategoryBackup = shop.Categories[0],
            ItemsBackup = new() { [0] = shop.Itens[0] },
            ItemListBackup = shop.Itens
        });
        var settings = new GshopSettings
        {
            AutoSave = true, SaveFromSeconds = 42,
            LastGshopLoadedFrom = "original", LastBackupFrom = "backup"
        };
        var expectedShop = ShopFixture.Export(shop);
        var expectedTracking = JsonSerializer.Serialize(tracking, ApplicationSettings.IdentedJson);
        var expectedSettings = JsonSerializer.Serialize(settings, ApplicationSettings.IdentedJson);
        var snapshot = SessionSnapshot.Capture(shop, tracking, settings);
        shop.Timestamp = DateTime.UtcNow;
        shop.Categories[0].Title = "Changed";
        shop.Categories[0].SubCategories.Clear();
        shop.Itens[0].Name = "Changed";
        shop.Itens[0].Sales[0].Price++;
        shop.Itens[0].OwnerNpcs[0]++;
        shop.Itens.Clear();
        tracking.Clear();
        settings.AutoSave = false;
        settings.SaveFromSeconds = 999;
        settings.LastGshopLoadedFrom = settings.LastBackupFrom = "Changed";
        await snapshot.WriteAsync(directory.Path);
        Assert.Equal(expectedShop, File.ReadAllBytes(Path.Combine(directory.Path, "gshop_temp.data")));
        Assert.Equal(expectedTracking, File.ReadAllText(Path.Combine(directory.Path, "tracking.json")));
        Assert.Equal(expectedSettings, File.ReadAllText(Path.Combine(directory.Path, "settings.json")));
    }

    [Fact]
    public async Task SavedSnapshotHistoryCanUndoAndRedoAfterLiveSessionChanges()
    {
        using var directory = new TestDirectory();
        var shop = ShopFixture.Create(2);
        var tracking = new ShopTracking();
        var original = ShopFixture.Export(shop);
        var changed = shop.Itens[0].Clone();
        changed.Name = "Snapshot edit";
        UpdateItem.Handle(shop.Itens, changed, shop.Itens[0], tracking);
        var edited = ShopFixture.Export(shop);
        var snapshot = SessionSnapshot.Capture(shop, tracking, new GshopSettings());
        tracking.Undo(shop);
        await snapshot.WriteAsync(directory.Path);
        var loaded = ContinueGshop.Handle(directory.Path);
        var history = ShopTracking.Load(Path.Combine(directory.Path, "tracking.json"));
        Assert.Equal(edited, ShopFixture.Export(loaded));
        history.Undo(loaded);
        Assert.Equal(original, ShopFixture.Export(loaded));
        history.Redo(loaded);
        Assert.Equal(edited, ShopFixture.Export(loaded));
    }
}
