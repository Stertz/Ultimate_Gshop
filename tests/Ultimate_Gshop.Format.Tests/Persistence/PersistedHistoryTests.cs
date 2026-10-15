using System.Text.Json;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Itens.Services;

namespace Ultimate_Gshop.Format.Tests;

public sealed class PersistedHistoryTests
{
    [Fact]
    public void ReloadedUndoHistoryKeepsTheLatestChangeOnTop()
    {
        var shop = ShopFixture.Create(1);
        var history = new ShopTracking();
        Rename(shop, history, "First edit");
        Rename(shop, history, "Second edit");

        var json = JsonSerializer.Serialize(history);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("First edit", document.RootElement.GetProperty("UndoChanges")[0]
            .GetProperty("ItemBackup").GetProperty("Name").GetString());
        var loaded = JsonSerializer.Deserialize<ShopTracking>(json)!;

        loaded.Undo(shop);
        Assert.Equal("First edit", shop.Itens[0].Name);
        loaded.Undo(shop);
        Assert.Equal("中文 Item 0", shop.Itens[0].Name);
        loaded.Redo(shop);
        Assert.Equal("First edit", shop.Itens[0].Name);
        loaded.Redo(shop);
        Assert.Equal("Second edit", shop.Itens[0].Name);
    }

    [Fact]
    public void ReloadedRedoHistoryKeepsTheNextChangeOnTop()
    {
        var shop = ShopFixture.Create(1);
        var history = new ShopTracking();
        Rename(shop, history, "First edit");
        Rename(shop, history, "Second edit");
        history.Undo(shop);
        history.Undo(shop);
        var loaded = JsonSerializer.Deserialize<ShopTracking>(JsonSerializer.Serialize(history))!;

        loaded.Redo(shop);
        Assert.Equal("First edit", shop.Itens[0].Name);
        loaded.Redo(shop);
        Assert.Equal("Second edit", shop.Itens[0].Name);
    }

    private static void Rename(Shop shop, ShopTracking history, string name)
    {
        var current = shop.Itens[0];
        var changed = current.Clone();
        changed.Name = name;
        UpdateItem.Handle(shop.Itens, changed, current, history);
    }
}
