using Ultimate_Gshop.Gshop.Services;

namespace Ultimate_Gshop.Format.Tests;

public sealed class ImportSafetyTests
{
    [Fact]
    public void ImportResetsPreviousHistoryAndPreservesUserPreferences()
    {
        using var directory = new TestDirectory();
        var source = Path.Combine(directory.Path, "source.data");
        var session = Path.Combine(directory.Path, "session");
        Directory.CreateDirectory(session);
        File.WriteAllBytes(source, ShopFixture.Export(ShopFixture.Create(1)));
        var history = new Ultimate_Gshop.Gshop.ShopTracking();
        history.ItemCreated(0, ShopFixture.CreateItem());
        Ultimate_Gshop.Gshop.ShopTracking.Save(history, Path.Combine(session, "tracking.json"));
        var settings = new Ultimate_Gshop.Gshop.GshopSettings { AutoSave = true, SaveFromSeconds = 45 };
        Ultimate_Gshop.Gshop.GshopSettings.Save(settings, Path.Combine(session, "settings.json"));

        ImportGshop.Handle(source, session);

        var loadedHistory = System.Text.Json.JsonSerializer.Deserialize<Ultimate_Gshop.Gshop.ShopTracking>(
            File.ReadAllText(Path.Combine(session, "tracking.json")))!;
        var loadedSettings = System.Text.Json.JsonSerializer.Deserialize<Ultimate_Gshop.Gshop.GshopSettings>(
            File.ReadAllText(Path.Combine(session, "settings.json")))!;
        Assert.False(loadedHistory.CanUndo);
        Assert.False(loadedHistory.CanRedo);
        Assert.True(loadedSettings.AutoSave);
        Assert.Equal(45, loadedSettings.SaveFromSeconds);
        Assert.Equal(source, loadedSettings.LastGshopLoadedFrom);
    }

    [Fact]
    public void TheExistingWorkingFileCanBeImportedWithoutCopyingItOntoItself()
    {
        using var directory = new TestDirectory();
        var source = Path.Combine(directory.Path, "gshop_temp.data");
        var original = ShopFixture.Export(ShopFixture.Create(1));
        File.WriteAllBytes(source, original);
        Assert.Single(ImportGshop.Handle(source, directory.Path).Itens);
        Assert.Equal(original, File.ReadAllBytes(source));
        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public void MalformedImportPreservesBothExistingSessionFilesAndMetadata()
    {
        using var directory = new TestDirectory();
        var session = Path.Combine(directory.Path, "session");
        Directory.CreateDirectory(session);
        var existing = ShopFixture.Export(ShopFixture.Create(1));
        foreach (var name in new[] { "gshop_before.data", "gshop_temp.data" })
            File.WriteAllBytes(Path.Combine(session, name), existing);
        File.WriteAllText(Path.Combine(session, "tracking.json"), "existing history");
        File.WriteAllText(Path.Combine(session, "settings.json"), "existing settings");
        var malformed = Path.Combine(directory.Path, "invalid.data");
        File.WriteAllBytes(malformed, [1, 2, 3]);

        Assert.ThrowsAny<Exception>(() => ImportGshop.Handle(malformed, session));

        Assert.Equal(existing, File.ReadAllBytes(Path.Combine(session, "gshop_before.data")));
        Assert.Equal(existing, File.ReadAllBytes(Path.Combine(session, "gshop_temp.data")));
        Assert.Equal("existing history", File.ReadAllText(Path.Combine(session, "tracking.json")));
        Assert.Equal("existing settings", File.ReadAllText(Path.Combine(session, "settings.json")));
        Assert.Empty(Directory.EnumerateFiles(session, "*.tmp"));
    }

    [Fact]
    public void ValidImportKeepsOriginalBytesInBothSessionFiles()
    {
        using var directory = new TestDirectory();
        var source = Path.Combine(directory.Path, "source.data");
        var session = Path.Combine(directory.Path, "session");
        var shop = ShopFixture.Create(1);
        shop.Itens[0].Place = 42;
        shop.Itens[0].Sales[0].Control = 77;
        var original = ShopFixture.Export(shop);
        File.WriteAllBytes(source, original);

        var imported = ImportGshop.Handle(source, session);

        Assert.Single(imported.Itens);
        Assert.Equal(original, File.ReadAllBytes(Path.Combine(session, "gshop_before.data")));
        Assert.Equal(original, File.ReadAllBytes(Path.Combine(session, "gshop_temp.data")));
        Assert.Equal(original, File.ReadAllBytes(source));
        Assert.Empty(Directory.EnumerateFiles(session, "*.tmp"));
    }
}
