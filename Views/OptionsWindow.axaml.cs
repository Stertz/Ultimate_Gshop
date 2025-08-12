using Avalonia.Controls;
using Avalonia.Interactivity;
using Ultimate_Gshop.Gshop;
namespace Ultimate_Gshop.Views;

public partial class OptionsWindow : Window
{
    private GshopSettings _settings;
    public OptionsWindow(GshopSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        AutoSaveToggleButton.IsChecked = _settings.AutoSave;
        SaveFromSeconds = _settings.SaveFromSeconds;
    }

    public bool? AutoSave { get; set; }
    public int? SaveFromSeconds { get; set; }

    private void OnAutoSaveToggle(object sender, RoutedEventArgs e)
    {
        AutoSave = AutoSaveToggleButton.IsChecked;
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        _settings.AutoSave = (bool)AutoSave!;
        _settings.SaveFromSeconds = Convert.ToInt32(SaveFromSecondsTextBox.Text);
        GshopSettings.Save(_settings);
        this.Close();
    }
}