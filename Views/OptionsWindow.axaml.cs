using Avalonia.Controls;
using Avalonia.Interactivity;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Views;

public partial class OptionsWindow : Window
{
    private readonly GshopSettings _original;
    private readonly string _sessionDirectory;
    private bool _saving;
    private bool _closed;
    public GshopSettings Settings { get; }

    public OptionsWindow() : this(GshopSettings.Load(), ApplicationSettings.AppFolder) { }
    public OptionsWindow(GshopSettings settings) : this(settings, ApplicationSettings.AppFolder) { }

    internal OptionsWindow(GshopSettings settings, string sessionDirectory)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _original = settings;
        _sessionDirectory = Path.GetFullPath(sessionDirectory);
        Settings = new GshopSettings
        {
            AutoSave = settings.AutoSave, SaveFromSeconds = settings.SaveFromSeconds,
            LastBackupFrom = settings.LastBackupFrom, LastGshopLoadedFrom = settings.LastGshopLoadedFrom
        };
        InitializeComponent();
        DataContext = Settings;
    }

    private async void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_closed || _saving) return;
        _saving = true;
        try
        {
            var text = SaveFromSecondsTextBox.Text;
            if (string.IsNullOrWhiteSpace(text)) Settings.SaveFromSeconds = null;
            else if (int.TryParse(text, out var seconds) && seconds >= 30) Settings.SaveFromSeconds = seconds;
            else throw new ArgumentException("The autosave interval must be at least 30 seconds.");

            // Persist the draft first; a canceled or failed dialog cannot change live preferences.
            Settings.LastBackupFrom = _original.LastBackupFrom;
            Settings.LastGshopLoadedFrom = _original.LastGshopLoadedFrom;
            GshopSettings.Save(Settings, Path.Combine(_sessionDirectory, "settings.json"));
            _original.AutoSave = Settings.AutoSave;
            _original.SaveFromSeconds = Settings.SaveFromSeconds;
            Close();
        }
        catch (Exception error) when (OperationErrors.IsRecoverable(error))
        {
            await OperationErrors.ShowAsync(this, "Could not save options", error);
        }
        finally { _saving = false; }
    }

    protected override void OnClosed(EventArgs e)
    {
        _closed = true;
        base.OnClosed(e);
    }
}
