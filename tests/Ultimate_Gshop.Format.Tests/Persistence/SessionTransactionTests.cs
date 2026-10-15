using System.Text;
using System.Text.Json;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Misc;
using Ultimate_Gshop.Gshop.Services;

namespace Ultimate_Gshop.Format.Tests;

public sealed class SessionTransactionTests
{
    [Fact]
    public void ContinuingASessionRecoversItsBinaryBeforeParsingIt()
    {
        using var directory = new TestDirectory();
        var journal = PrepareJournal(directory.Path, new SessionTransaction.Entry("gshop_temp.data", true));
        var original = ShopFixture.Export(ShopFixture.Create(1));
        File.WriteAllBytes(Path.Combine(journal, "gshop_temp.data.previous"), original);
        File.WriteAllBytes(Path.Combine(directory.Path, "gshop_temp.data"), [1, 2, 3]);
        Assert.Single(ContinueGshop.Handle(directory.Path).Itens);
        Assert.Equal(original, File.ReadAllBytes(Path.Combine(directory.Path, "gshop_temp.data")));
        Assert.False(Directory.Exists(journal));
    }

    [Fact]
    public void ImportingTheWorkingFileRecoversItBeforeTakingTheSnapshot()
    {
        using var directory = new TestDirectory();
        var journal = PrepareJournal(directory.Path, new SessionTransaction.Entry("gshop_temp.data", true));
        var original = ShopFixture.Export(ShopFixture.Create(1));
        File.WriteAllBytes(Path.Combine(journal, "gshop_temp.data.previous"), original);
        var working = Path.Combine(directory.Path, "gshop_temp.data");
        File.WriteAllBytes(working, [1, 2, 3]);
        Assert.Single(ImportGshop.Handle(working, directory.Path).Itens);
        Assert.Equal(original, File.ReadAllBytes(Path.Combine(directory.Path, "gshop_before.data")));
    }

    [Fact]
    public async Task ConcurrentSavesCannotMixGenerations()
    {
        using var directory = new TestDirectory();
        await Task.WhenAll(Enumerable.Range(0, 8).Select(generation => Task.Run(() =>
        {
            var value = Encoding.UTF8.GetBytes($"generation {generation}");
            SessionTransaction.Write(directory.Path, new Dictionary<string, Action<Stream>>
            {
                ["tracking.json"] = stream => stream.Write(value),
                ["settings.json"] = stream => stream.Write(value)
            });
        })));
        Assert.Equal(File.ReadAllBytes(Path.Combine(directory.Path, "tracking.json")),
            File.ReadAllBytes(Path.Combine(directory.Path, "settings.json")));
    }

    [Fact]
    public void AnExternallyHeldSessionLockPreventsReplacement()
    {
        using var directory = new TestDirectory();
        using var heldLock = new FileStream(Path.Combine(directory.Path, ".session.lock"),
            FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        Assert.Throws<IOException>(() => SessionTransaction.Write(directory.Path, Writes()));
        Assert.False(File.Exists(Path.Combine(directory.Path, "tracking.json")));
    }

    [Fact]
    public void CorruptMetadataIsReportedAndRetainedInsteadOfSilentlyReplaced()
    {
        using var directory = new TestDirectory();
        var settings = Path.Combine(directory.Path, "settings.json");
        var tracking = Path.Combine(directory.Path, "tracking.json");
        File.WriteAllText(settings, "{ broken settings");
        File.WriteAllText(tracking, "{ broken history");
        Assert.Throws<JsonException>(() => GshopSettings.Load(settings));
        Assert.Throws<JsonException>(() => ShopTracking.Load(tracking));
        Assert.Equal("{ broken settings", File.ReadAllText(settings));
        Assert.Equal("{ broken history", File.ReadAllText(tracking));
    }

    [Fact]
    public void AllBackupsAreValidatedBeforeRestoringAnyFile()
    {
        using var directory = new TestDirectory();
        var journal = PrepareJournal(directory.Path, new SessionTransaction.Entry("settings.json", true),
            new SessionTransaction.Entry("tracking.json", true));
        File.WriteAllText(Path.Combine(journal, "settings.json.previous"), "old settings");
        File.WriteAllText(Path.Combine(directory.Path, "settings.json"), "current settings");
        Assert.Throws<InvalidDataException>(() => SessionTransaction.Read(directory.Path, () => true));
        Assert.Equal("current settings", File.ReadAllText(Path.Combine(directory.Path, "settings.json")));
        Assert.True(File.Exists(Path.Combine(journal, "settings.json.previous")));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void ReplacementFailureRollsBackEveryOriginalFile(int failureAfter)
    {
        using var directory = new TestDirectory();
        File.WriteAllText(Path.Combine(directory.Path, "tracking.json"), "old history");
        File.WriteAllText(Path.Combine(directory.Path, "settings.json"), "old settings");

        Assert.Throws<IOException>(() => SessionTransaction.Write(directory.Path, Writes(), step =>
        {
            if (step == failureAfter) throw new IOException("Injected replacement failure.");
        }));

        Assert.Equal("old history", File.ReadAllText(Path.Combine(directory.Path, "tracking.json")));
        Assert.Equal("old settings", File.ReadAllText(Path.Combine(directory.Path, "settings.json")));
        Assert.False(Directory.Exists(Path.Combine(directory.Path, ".session-transaction")));
        SessionTransaction.Write(directory.Path, Writes());
        Assert.Equal("new history", File.ReadAllText(Path.Combine(directory.Path, "tracking.json")));
    }

    [Fact]
    public void FailedFirstSaveRemovesOnlyNewSessionFiles()
    {
        using var directory = new TestDirectory();
        File.WriteAllText(Path.Combine(directory.Path, "unrelated.txt"), "keep");
        Assert.Throws<IOException>(() => SessionTransaction.Write(directory.Path, Writes(), _ =>
            throw new IOException("Injected replacement failure.")));
        Assert.False(File.Exists(Path.Combine(directory.Path, "tracking.json")));
        Assert.False(File.Exists(Path.Combine(directory.Path, "settings.json")));
        Assert.Equal("keep", File.ReadAllText(Path.Combine(directory.Path, "unrelated.txt")));
    }

    [Fact]
    public void StagingFailureLeavesEveryOriginalFileUnchanged()
    {
        using var directory = new TestDirectory();
        File.WriteAllText(Path.Combine(directory.Path, "tracking.json"), "old history");
        var writes = Writes();
        writes["settings.json"] = stream =>
        {
            stream.WriteByte(1);
            throw new IOException("Injected serialization failure.");
        };
        Assert.Throws<IOException>(() => SessionTransaction.Write(directory.Path, writes));
        Assert.Equal("old history", File.ReadAllText(Path.Combine(directory.Path, "tracking.json")));
        Assert.False(File.Exists(Path.Combine(directory.Path, "settings.json")));
        Assert.False(Directory.Exists(Path.Combine(directory.Path, ".session-transaction")));
    }

    [Fact]
    public void AnInterruptedReplacementIsRecoveredBeforeReadingSettings()
    {
        using var directory = new TestDirectory();
        var journal = PrepareJournal(directory.Path,
            new SessionTransaction.Entry("settings.json", true),
            new SessionTransaction.Entry("tracking.json", false));
        File.WriteAllText(Path.Combine(journal, "settings.json.previous"), "{\"SaveFromSeconds\":45}");
        File.WriteAllText(Path.Combine(directory.Path, "settings.json"), "{\"SaveFromSeconds\":90}");
        File.WriteAllText(Path.Combine(directory.Path, "tracking.json"), "partial generation");

        var loaded = GshopSettings.Load(Path.Combine(directory.Path, "settings.json"));

        Assert.Equal(45, loaded.SaveFromSeconds);
        Assert.False(File.Exists(Path.Combine(directory.Path, "tracking.json")));
        Assert.False(Directory.Exists(journal));
    }

    [Fact]
    public void AResolvedTransactionKeepsNewFilesEvenWhenCleanupWasInterrupted()
    {
        using var directory = new TestDirectory();
        var journal = PrepareJournal(directory.Path, new SessionTransaction.Entry("settings.json", true));
        File.WriteAllText(Path.Combine(journal, "resolved"), "1");
        File.WriteAllText(Path.Combine(directory.Path, "settings.json"), "{\"SaveFromSeconds\":90}");
        Assert.Equal(90, GshopSettings.Load(Path.Combine(directory.Path, "settings.json")).SaveFromSeconds);
        Assert.False(Directory.Exists(journal));
    }

    [Fact]
    public void AnIncompletePreparationDoesNotChangeDestinationFiles()
    {
        using var directory = new TestDirectory();
        var journal = Path.Combine(directory.Path, ".session-transaction");
        Directory.CreateDirectory(journal);
        File.WriteAllText(Path.Combine(journal, "prepared.json"), "incomplete journal");
        File.WriteAllText(Path.Combine(journal, "settings.json.next"), "partial settings");
        File.WriteAllText(Path.Combine(directory.Path, "settings.json"), "{\"SaveFromSeconds\":45}");
        Assert.Equal(45, GshopSettings.Load(Path.Combine(directory.Path, "settings.json")).SaveFromSeconds);
        Assert.False(Directory.Exists(journal));
    }

    [Fact]
    public void MissingRecoveryBackupsPreserveTheJournalAndCurrentFiles()
    {
        using var directory = new TestDirectory();
        var journal = PrepareJournal(directory.Path, new SessionTransaction.Entry("settings.json", true));
        File.WriteAllText(Path.Combine(directory.Path, "settings.json"), "current bytes");
        Assert.Throws<InvalidDataException>(() => GshopSettings.Load(Path.Combine(directory.Path, "settings.json")));
        Assert.Equal("current bytes", File.ReadAllText(Path.Combine(directory.Path, "settings.json")));
        Assert.True(File.Exists(Path.Combine(journal, "pending.json")));
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("Resources/icon.png")]
    [InlineData("SETTINGS.JSON")]
    public void RecoveryCannotTargetNamesOutsideTheSessionProtocol(string name)
    {
        using var directory = new TestDirectory();
        var journal = PrepareJournal(directory.Path, new SessionTransaction.Entry(name, false));
        Assert.Throws<InvalidDataException>(() => SessionTransaction.Read(directory.Path, () => true));
        Assert.True(File.Exists(Path.Combine(journal, "pending.json")));
    }

    [Fact]
    public void StartingANewShopResetsHistoryAndSourceWhileKeepingPreferences()
    {
        using var directory = new TestDirectory();
        var history = new ShopTracking();
        history.ItemCreated(0, ShopFixture.CreateItem());
        ShopTracking.Save(history, Path.Combine(directory.Path, "tracking.json"));
        var settings = new GshopSettings { AutoSave = true, SaveFromSeconds = 45, LastGshopLoadedFrom = "old.data" };
        var shop = ShopFixture.Create(1);
        StartGshopSession.Handle(shop, settings, directory.Path);
        var loadedHistory = ShopTracking.Load(Path.Combine(directory.Path, "tracking.json"));
        var loadedSettings = GshopSettings.Load(Path.Combine(directory.Path, "settings.json"));
        Assert.False(loadedHistory.CanUndo);
        Assert.True(loadedSettings.AutoSave);
        Assert.Equal(45, loadedSettings.SaveFromSeconds);
        Assert.Null(loadedSettings.LastGshopLoadedFrom);
        Assert.Null(settings.LastGshopLoadedFrom);
        Assert.Equal(ShopFixture.Export(shop), File.ReadAllBytes(Path.Combine(directory.Path, "gshop_temp.data")));
        Assert.Equal(ShopFixture.Export(shop), File.ReadAllBytes(Path.Combine(directory.Path, "gshop_before.data")));
    }

    [Fact]
    public void ClearingInMemoryHistoryInvalidatesBothStacksAndNotifiesButtons()
    {
        var history = new ShopTracking();
        history.UndoChanges.Push(new ShopChange());
        history.RedoChanges.Push(new ShopChange());
        var changes = new List<string?>();
        history.PropertyChanged += (_, args) => changes.Add(args.PropertyName);
        history.Clear();
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Equal(new[] { "CanUndo", "CanRedo" }, changes);
    }

    private static string PrepareJournal(string root, params SessionTransaction.Entry[] entries)
    {
        var journal = Path.Combine(root, ".session-transaction");
        Directory.CreateDirectory(journal);
        File.WriteAllText(Path.Combine(journal, "pending.json"), JsonSerializer.Serialize(entries));
        return journal;
    }

    private static Dictionary<string, Action<Stream>> Writes() => new()
    {
        ["tracking.json"] = stream => stream.Write(Encoding.UTF8.GetBytes("new history")),
        ["settings.json"] = stream => stream.Write(Encoding.UTF8.GetBytes("new settings"))
    };
}
