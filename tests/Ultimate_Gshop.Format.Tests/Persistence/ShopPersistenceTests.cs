using System.Text.Json;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;

namespace Ultimate_Gshop.Format.Tests;

public sealed class ShopPersistenceTests
{
    [Fact]
    public void ASettingsWriteFailureCannotCommitANewBinaryOrHistory()
    {
        using var directory = new TestDirectory();
        var history = new ShopTracking();
        SaveGshop.Handle(ShopFixture.Create(1), history, new GshopSettings(), directory.Path);
        var binaryPath = Path.Combine(directory.Path, "gshop.data");
        var historyPath = Path.Combine(directory.Path, "tracking.json");
        var beforeBinary = File.ReadAllBytes(binaryPath);
        var beforeHistory = File.ReadAllBytes(historyPath);
        var settingsPath = Path.Combine(directory.Path, "settings.json");
        File.Delete(settingsPath);
        Directory.CreateDirectory(settingsPath);
        history.ItemCreated(1, ShopFixture.CreateItem(1));

        Assert.ThrowsAny<Exception>(() =>
            SaveGshop.Handle(ShopFixture.Create(2), history, new GshopSettings(), directory.Path));

        Assert.Equal(beforeBinary, File.ReadAllBytes(binaryPath));
        Assert.Equal(beforeHistory, File.ReadAllBytes(historyPath));
    }

    [Fact]
    public void ANewShopCanBeSavedAndLoadedWithoutEditingEveryItemFirst()
    {
        using var directory = new TestDirectory();
        var shop = NewEmptyGshop.Handle();
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        SaveGshop.Handle(shop, new ShopTracking(), new GshopSettings(), directory.Path);
        var loaded = LoadGshop.Handle(Path.Combine(directory.Path, "gshop.data"));

        Assert.Equal(8, loaded.Itens.Count);
        Assert.Equal(Enumerable.Range(1, 8), loaded.Itens.Select(item => item.ID));
        Assert.Equal(Enumerable.Range(0, 8), loaded.Itens.Select(item => item.Place));
        Assert.All(loaded.Itens, item =>
        {
            Assert.Equal(4, item.Sales.Count);
            Assert.Equal(8, item.OwnerNpcs.Length);
        });
        Assert.Equal(ShopFixture.Export(shop), ShopFixture.Export(loaded));
    }

    [Fact]
    public void SaveUsesTheRequestedDirectoryAndPreservesSettingsPropertyNames()
    {
        using var directory = new TestDirectory();
        var shop = ShopFixture.Create();
        var settings = new GshopSettings { AutoSave = true, SaveFromSeconds = 30, LastGshopLoadedFrom = @"C:\game\gshop.data" };
        var history = new ShopTracking();
        history.ItemCreated(0, shop.Itens[0]);

        SaveGshop.Handle(shop, history, settings, directory.Path);

        Assert.Equal(ShopFixture.Export(shop), File.ReadAllBytes(Path.Combine(directory.Path, "gshop.data")));
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory.Path, "settings.json")));
        Assert.True(json.RootElement.GetProperty("AutoSave").GetBoolean());
        Assert.Equal(30, json.RootElement.GetProperty("SaveFromSeconds").GetInt32());
        Assert.Equal(settings.LastGshopLoadedFrom, json.RootElement.GetProperty("LastGshopLoadedFrom").GetString());
        Assert.True(File.Exists(Path.Combine(directory.Path, "tracking.json")));
    }

    [Fact]
    public void SavingASmallerShopRemovesOldTrailingBytes()
    {
        using var directory = new TestDirectory();
        var history = new ShopTracking();
        var settings = new GshopSettings();
        SaveGshop.Handle(ShopFixture.Create(2), history, settings, directory.Path);
        var smaller = ShopFixture.Create(0);

        SaveGshop.Handle(smaller, history, settings, directory.Path);

        Assert.Equal(ShopFixture.Export(smaller), File.ReadAllBytes(Path.Combine(directory.Path, "gshop.data")));
    }

    [Fact]
    public void SerializationFailurePreservesTheExistingFileAndLeavesNoTemporaryFiles()
    {
        using var directory = new TestDirectory();
        var history = new ShopTracking();
        var settings = new GshopSettings();
        var original = ShopFixture.Create(2);
        SaveGshop.Handle(original, history, settings, directory.Path);
        var originalBytes = File.ReadAllBytes(Path.Combine(directory.Path, "gshop.data"));
        var invalid = ShopFixture.Create(1);
        invalid.Itens[0].Sales.Clear();

        Assert.Throws<ArgumentOutOfRangeException>(() => SaveGshop.Handle(invalid, history, settings, directory.Path));

        Assert.Equal(originalBytes, File.ReadAllBytes(Path.Combine(directory.Path, "gshop.data")));
        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*.tmp"));
    }
}
