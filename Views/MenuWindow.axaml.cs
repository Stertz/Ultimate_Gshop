using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Misc;
using Ultimate_Gshop.Views;

namespace Ultimate_Gshop;

public partial class MenuWindow : Window
{
    private readonly Shop? _shop;
    private GshopSettings? _settings;
    private readonly ShopTracking? _tracking;
    private readonly string _sessionDirectory;
    private readonly Func<Window, Task<string?>> _selectImportFile;
    private readonly Func<Window, Task<string?>> _selectExportFolder;
    private bool _operationInProgress;
    private bool _closed;

    public MenuWindow() : this(null, null, null, ApplicationSettings.AppFolder) { }

    public MenuWindow(Shop shop, GshopSettings settings, ShopTracking tracking)
        : this(shop, settings, tracking, ApplicationSettings.AppFolder) { }

    internal MenuWindow(Shop? shop, GshopSettings? settings, ShopTracking? tracking, string sessionDirectory,
        Func<Window, Task<string?>>? selectImportFile = null, Func<Window, Task<string?>>? selectExportFolder = null)
    {
        _shop = shop;
        _settings = settings;
        _tracking = tracking;
        _sessionDirectory = Path.GetFullPath(sessionDirectory);
        _selectImportFile = selectImportFile ?? SelectImportFileAsync;
        _selectExportFolder = selectExportFolder ?? SetGshopFileFromOS.Handle;
        InitializeComponent();
        Cursor = ApplicationSettings.InitializeCustomCursor();
        btnExportServer.IsEnabled = shop is not null;
        btnExportClient.IsEnabled = shop is not null;
        btnContinue.IsEnabled = shop is not null || File.Exists(Path.Combine(_sessionDirectory, "gshop_temp.data"));
    }

    private static async Task<string?> SelectImportFileAsync(Window owner)
    {
        var file = await GetGshopFileFromOS.Handle(owner);
        return file is null ? null : file.TryGetLocalPath()
            ?? throw new InvalidOperationException("The selected file does not have a local path.");
    }

    private GshopSettings GetSettings() =>
        _settings ??= GshopSettings.Load(Path.Combine(_sessionDirectory, "settings.json"));

    private async Task ExecuteAsync(string failureTitle, Func<Task> operation)
    {
        if (_closed || _operationInProgress) return;
        _operationInProgress = true;
        try { await operation(); }
        catch (Exception error) when (OperationErrors.IsRecoverable(error))
        {
            if (!_closed) await OperationErrors.ShowAsync(this, failureTitle, error);
        }
        finally { _operationInProgress = false; }
    }

    private void MenuWindow_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Escape) Close(_shop);
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e) => Close(_shop);

    protected override void OnClosed(EventArgs e)
    {
        _closed = true;
        base.OnClosed(e);
    }

    private async void OnExportServerClicked(object sender, RoutedEventArgs e) => await ExportAsync(GshopExportTarget.Server);
    private async void OnExportClientClicked(object sender, RoutedEventArgs e) => await ExportAsync(GshopExportTarget.Client);

    private Task ExportAsync(GshopExportTarget target) => ExecuteAsync("Could not export the shop", async () =>
    {
        var folder = await _selectExportFolder(this);
        if (_closed || folder is null) return;
        if (_shop is null) throw new InvalidOperationException("Shop is not initialized.");
        var name = target == GshopExportTarget.Server ? "gshopsev.data" : "gshop.data";
        var destination = Path.Combine(folder, name);
        if (File.Exists(destination))
        {
            var choice = await new CustomMessageBox("Replace existing file?", name,
                CustomMessageBox.MessageBoxIcon.Question, "Replace", "Cancel").ShowDialogAsync(this);
            if (_closed || choice != "Replace") return;
        }
        SaveGshop.HandleSession(_shop, _tracking ?? new ShopTracking(), GetSettings(), _sessionDirectory);
        ExportGshopFile.Handle(_shop, destination, target);
    });

    private async void OnImportGshopClicked(object sender, RoutedEventArgs e) =>
        await ExecuteAsync("Could not import the shop", async () =>
        {
            var path = await _selectImportFile(this);
            if (_closed || path is null) return;
            var settings = GetSettings();
            var previousBackup = settings.LastBackupFrom;
            Shop imported;
            try
            {
                // Back up only after the private import snapshot is validated, before replacing the session.
                imported = ImportGshop.Handle(path, _sessionDirectory, settings, BackupCurrentShop);
            }
            catch { settings.LastBackupFrom = previousBackup; throw; }
            _tracking?.Clear();
            Close(imported);
        });

    private async void OnContinueGshopClicked(object sender, RoutedEventArgs e) =>
        await ExecuteAsync("Could not continue the shop", () =>
        {
            Close(_shop ?? ContinueGshop.Handle(_sessionDirectory));
            return Task.CompletedTask;
        });

    private async void OnNewGshopClicked(object sender, RoutedEventArgs e) =>
        await ExecuteAsync("Could not create the shop", () =>
        {
            var settings = GetSettings();
            var previousBackup = settings.LastBackupFrom;
            var shop = NewEmptyGshop.Handle();
            try
            {
                BackupCurrentShop();
                StartGshopSession.Handle(shop, settings, _sessionDirectory);
            }
            catch { settings.LastBackupFrom = previousBackup; throw; }
            _tracking?.Clear();
            Close(shop);
            return Task.CompletedTask;
        });

    private async void OnOptionsClicked(object sender, RoutedEventArgs e) =>
        await ExecuteAsync("Could not open options", async () =>
        {
            var options = new OptionsWindow(GetSettings(), _sessionDirectory)
            {
                WindowStartupLocation = WindowStartupLocation.Manual
            };
            options.Position = new PixelPoint(
                (int)(Position.X + (Bounds.Width - options.Width) / 2),
                (int)(Position.Y + (Bounds.Height - options.Height) / 2));
            await options.ShowDialog(this);
        });

    private void BackupCurrentShop()
    {
        if (_shop is null || !File.Exists(Path.Combine(_sessionDirectory, "gshop_temp.data"))) return;
        var folder = Path.Combine(_sessionDirectory, "backups",
            $"gshop_{DateTime.UtcNow:yyyyMMdd_HHmmss_fffffff}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        var settings = GetSettings();
        SaveGshop.Handle(_shop, _tracking ?? new ShopTracking(), settings, folder);
        settings.LastBackupFrom = folder;
    }
}
