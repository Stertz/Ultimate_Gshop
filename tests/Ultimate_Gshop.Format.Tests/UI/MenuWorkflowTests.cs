using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Itens.Services;
using Ultimate_Gshop.Views;

namespace Ultimate_Gshop.Format.Tests;

[Collection("Image rendering")]
public sealed class MenuWorkflowTests(ImageRenderingFixture fixture)
{
    private static void Click(MenuWindow menu, string text) => menu.GetVisualDescendants().OfType<Button>()
        .Single(button => button.Content is TextBlock label && label.Text == text)
        .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static void Answer(MenuWindow menu, int index = 0) => menu.OwnedWindows.OfType<CustomMessageBox>().Single()
        .FindControl<Panel>("ButtonPanel")!.Children.OfType<Button>().ElementAt(index)
        .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static async Task PumpAsync() => await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ExportButtonsUseTheCorrectWriterAndSaveTheIsolatedSession(bool server) => fixture.Run(async () =>
    {
        using var session = new TestDirectory();
        using var destination = new TestDirectory();
        var shop = ShopFixture.Create();
        var history = new ShopTracking();
        var menu = new MenuWindow(shop, new GshopSettings(), history, session.Path,
            selectExportFolder: _ => Task.FromResult<string?>(destination.Path));
        try
        {
            menu.Show();
            Click(menu, server ? "Export Gshop Server" : "Export Gshop Client");
            await PumpAsync();
            Assert.Empty(menu.OwnedWindows);
            Assert.Equal(ShopFixture.Export(shop, server), File.ReadAllBytes(Path.Combine(destination.Path, server ? "gshopsev.data" : "gshop.data")));
            Assert.Equal(ShopFixture.Export(shop), File.ReadAllBytes(Path.Combine(session.Path, "gshop_temp.data")));
            Assert.True(menu.IsVisible);
        }
        finally { menu.Close(); }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task CancelledPickerDoesNotBackUpSaveOrChangeHistory(bool import) => fixture.Run(async () =>
    {
        using var session = new TestDirectory();
        var shop = ShopFixture.Create();
        var settings = new GshopSettings { LastBackupFrom = "Previous backup" };
        var history = new ShopTracking();
        InsertItem.Handle(shop.Itens, ShopFixture.CreateItem(2), history);
        SaveGshop.HandleSession(shop, history, settings, session.Path);
        var previous = File.ReadAllBytes(Path.Combine(session.Path, "gshop_temp.data"));
        var state = JsonSerializer.Serialize(history);
        var menu = new MenuWindow(shop, settings, history, session.Path,
            _ => Task.FromResult<string?>(null), _ => Task.FromResult<string?>(null));
        try
        {
            menu.Show();
            Click(menu, import ? "Import Gshop" : "Export Gshop Client");
            await PumpAsync();
            Assert.True(menu.IsVisible);
            Assert.Empty(menu.OwnedWindows);
            Assert.False(Directory.Exists(Path.Combine(session.Path, "backups")));
            Assert.Equal("Previous backup", settings.LastBackupFrom);
            Assert.Equal(state, JsonSerializer.Serialize(history));
            Assert.Equal(previous, File.ReadAllBytes(Path.Combine(session.Path, "gshop_temp.data")));
        }
        finally { menu.Close(); }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task DecliningOrClosingOverwritePreservesTheDestination(bool closeDialog) => fixture.Run(async () =>
    {
        using var session = new TestDirectory();
        using var destination = new TestDirectory();
        var file = Path.Combine(destination.Path, "gshop.data");
        File.WriteAllBytes(file, [1, 2, 3]);
        var menu = new MenuWindow(ShopFixture.Create(), new GshopSettings(), new ShopTracking(), session.Path,
            selectExportFolder: _ => Task.FromResult<string?>(destination.Path));
        try
        {
            menu.Show();
            Click(menu, "Export Gshop Client");
            if (closeDialog) menu.OwnedWindows.Single().Close(); else Answer(menu, 1);
            await PumpAsync();
            Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(file));
            Assert.False(File.Exists(Path.Combine(session.Path, "gshop_temp.data")));
            Assert.True(menu.IsVisible);
        }
        finally { menu.Close(); }
    });

    [Fact]
    public Task PickerFailureIsReportedOnceAndTheSameMenuCanRetry() => fixture.Run(async () =>
    {
        using var session = new TestDirectory();
        using var destination = new TestDirectory();
        var fail = true;
        var calls = 0;
        var shop = ShopFixture.Create();
        var menu = new MenuWindow(shop, new GshopSettings(), new ShopTracking(), session.Path,
            selectExportFolder: _ =>
            {
                calls++;
                return fail ? Task.FromException<string?>(new NotSupportedException("Picker unavailable")) : Task.FromResult<string?>(destination.Path);
            });
        try
        {
            menu.Show();
            Click(menu, "Export Gshop Client");
            Click(menu, "Export Gshop Client");
            Assert.Equal(1, calls);
            Assert.Single(menu.OwnedWindows);
            Assert.Equal("Could not export the shop", menu.OwnedWindows.OfType<CustomMessageBox>().Single().FindControl<TextBlock>("TitleBlock")!.Text);
            Answer(menu);
            await PumpAsync();
            fail = false;
            Click(menu, "Export Gshop Client");
            await PumpAsync();
            Assert.Equal(2, calls);
            Assert.Equal(ShopFixture.Export(shop), File.ReadAllBytes(Path.Combine(destination.Path, "gshop.data")));
        }
        finally { menu.Close(); }
    });

    [Fact]
    public Task ExportIoFailureKeepsTheMenuAndExistingDestinationIntact() => fixture.Run(async () =>
    {
        using var session = new TestDirectory();
        using var destination = new TestDirectory();
        var blocker = Path.Combine(destination.Path, "gshop.data");
        Directory.CreateDirectory(blocker);
        File.WriteAllText(Path.Combine(blocker, "keep.txt"), "Existing directory");
        var menu = new MenuWindow(ShopFixture.Create(), new GshopSettings(), new ShopTracking(), session.Path,
            selectExportFolder: _ => Task.FromResult<string?>(destination.Path));
        try
        {
            menu.Show();
            Click(menu, "Export Gshop Client");
            Assert.Single(menu.OwnedWindows);
            Answer(menu);
            await PumpAsync();
            Assert.True(menu.IsVisible);
            Assert.Equal("Existing directory", File.ReadAllText(Path.Combine(blocker, "keep.txt")));
            Assert.Empty(Directory.EnumerateFiles(destination.Path, ".*.tmp"));
        }
        finally { menu.Close(); }
    });

    [Fact]
    public Task MalformedImportPreservesSessionPreferencesAndHistoryWithoutCreatingABackup() => fixture.Run(async () =>
    {
        using var session = new TestDirectory();
        using var source = new TestDirectory();
        var path = Path.Combine(source.Path, "invalid.data");
        File.WriteAllBytes(path, [1, 2, 3]);
        var shop = ShopFixture.Create();
        var settings = new GshopSettings { LastBackupFrom = "Existing backup", LastGshopLoadedFrom = "Existing source" };
        var history = new ShopTracking();
        InsertItem.Handle(shop.Itens, ShopFixture.CreateItem(2), history);
        SaveGshop.HandleSession(shop, history, settings, session.Path);
        var previous = File.ReadAllBytes(Path.Combine(session.Path, "gshop_temp.data"));
        var state = JsonSerializer.Serialize(history);
        var menu = new MenuWindow(shop, settings, history, session.Path, _ => Task.FromResult<string?>(path));
        try
        {
            menu.Show();
            Click(menu, "Import Gshop");
            Answer(menu);
            await PumpAsync();
            Assert.True(menu.IsVisible);
            Assert.Equal(previous, File.ReadAllBytes(Path.Combine(session.Path, "gshop_temp.data")));
            Assert.Equal("Existing backup", settings.LastBackupFrom);
            Assert.Equal("Existing source", settings.LastGshopLoadedFrom);
            Assert.Equal(state, JsonSerializer.Serialize(history));
            Assert.False(Directory.Exists(Path.Combine(session.Path, "backups")));
            Assert.Empty(Directory.EnumerateFiles(session.Path, ".import.*.tmp"));
        }
        finally { menu.Close(); }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ValidImportBacksUpTheCurrentShopAndClearsHistoryOnlyAfterCommit(bool backupFails) => fixture.Run(async () =>
    {
        using var session = new TestDirectory();
        using var source = new TestDirectory();
        using var owner = new HeadlessHost(new Border());
        var path = Path.Combine(source.Path, "import.data");
        var incoming = ShopFixture.Create(1);
        incoming.Itens[0].Name = "Imported";
        var incomingBytes = ShopFixture.Export(incoming);
        File.WriteAllBytes(path, incomingBytes);
        var shop = ShopFixture.Create();
        var settings = new GshopSettings { AutoSave = true, SaveFromSeconds = 90, LastBackupFrom = "Previous" };
        var history = new ShopTracking();
        InsertItem.Handle(shop.Itens, ShopFixture.CreateItem(2), history);
        var previous = ShopFixture.Export(shop);
        SaveGshop.HandleSession(shop, history, settings, session.Path);
        if (backupFails) File.WriteAllText(Path.Combine(session.Path, "backups"), "Blocked");
        var menu = new MenuWindow(shop, settings, history, session.Path, _ => Task.FromResult<string?>(path));
        try
        {
            var result = menu.ShowDialog<Shop>(owner.Window);
            Click(menu, "Import Gshop");
            if (backupFails)
            {
                Answer(menu);
                await PumpAsync();
                Assert.True(menu.IsVisible);
                Assert.True(history.CanUndo);
                Assert.Equal("Previous", settings.LastBackupFrom);
                Assert.Equal(previous, File.ReadAllBytes(Path.Combine(session.Path, "gshop_temp.data")));
            }
            else
            {
                var loaded = await result.WaitAsync(TimeSpan.FromSeconds(3));
                Assert.Equal("Imported", Assert.Single(loaded.Itens).Name);
                Assert.Equal(incomingBytes, File.ReadAllBytes(Path.Combine(session.Path, "gshop_temp.data")));
                Assert.Equal(previous, File.ReadAllBytes(Path.Combine(settings.LastBackupFrom!, "gshop.data")));
                Assert.False(history.CanUndo);
                Assert.Equal(path, settings.LastGshopLoadedFrom);
                Assert.True(GshopSettings.Load(Path.Combine(session.Path, "settings.json")).AutoSave);
            }
            Assert.Empty(Directory.EnumerateFiles(session.Path, ".import.*.tmp"));
            Assert.Equal(incomingBytes, File.ReadAllBytes(path));
        }
        finally { menu.Close(); }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ClosedMenuIgnoresALatePickerResult(bool import) => fixture.Run(async () =>
    {
        using var session = new TestDirectory();
        using var selected = new TestDirectory();
        var pending = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var menu = new MenuWindow(ShopFixture.Create(), new GshopSettings(), new ShopTracking(), session.Path,
            _ => pending.Task, _ => pending.Task);
        menu.Show();
        Click(menu, import ? "Import Gshop" : "Export Gshop Client");
        menu.Close();
        pending.SetResult(selected.Path);
        await PumpAsync();
        Assert.Empty(Directory.EnumerateFileSystemEntries(session.Path));
        Assert.Empty(Directory.EnumerateFileSystemEntries(selected.Path));
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task StartupMenuCanCreateOrContinueAnIsolatedShop(bool continueExisting) => fixture.Run(async () =>
    {
        using var session = new TestDirectory();
        using var owner = new HeadlessHost(new Border());
        if (continueExisting) SaveGshop.HandleSession(ShopFixture.Create(1), new ShopTracking(), new GshopSettings(), session.Path);
        var menu = new MenuWindow(null, null, null, session.Path);
        try
        {
            var result = menu.ShowDialog<Shop>(owner.Window);
            Assert.False(menu.FindControl<Button>("btnExportClient")!.IsEnabled);
            Assert.Equal(continueExisting, menu.FindControl<Button>("btnContinue")!.IsEnabled);
            Click(menu, continueExisting ? "Continue Gshop" : "New Gshop");
            var shop = await result.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.Equal(continueExisting ? 1 : 8, shop.Itens.Count);
            Assert.True(File.Exists(Path.Combine(session.Path, "gshop_temp.data")));
        }
        finally { menu.Close(); }
    });

    [Fact]
    public Task StartupOptionsUseIsolatedPreferencesAndCancelWithoutWriting() => fixture.Run(async () =>
    {
        using var session = new TestDirectory();
        var menu = new MenuWindow(null, null, null, session.Path);
        try
        {
            menu.Show();
            Click(menu, "Options");
            var options = menu.OwnedWindows.OfType<OptionsWindow>().Single();
            Assert.IsType<GshopSettings>(options.DataContext);
            options.FindControl<Avalonia.Controls.Primitives.ToggleButton>("AutoSaveToggleButton")!.IsChecked = true;
            options.Close();
            await PumpAsync();
            Assert.True(menu.IsVisible);
            Assert.Empty(menu.OwnedWindows);
            Assert.False(File.Exists(Path.Combine(session.Path, "settings.json")));
        }
        finally { menu.Close(); }
    });
}
