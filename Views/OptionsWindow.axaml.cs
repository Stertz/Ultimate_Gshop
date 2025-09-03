using Avalonia.Controls;
using Avalonia.Interactivity;
using Ultimate_Gshop.Gshop;
namespace Ultimate_Gshop.Views;

public partial class OptionsWindow : Window
{
    public GshopSettings Settings { get; set; }
    public OptionsWindow()
    {
        InitializeComponent();
        Settings = GshopSettings.Load();
        DataContext = null;
    }
    public OptionsWindow(GshopSettings settings)
    {
        InitializeComponent();
        Settings = settings;
        DataContext = Settings;
    }
    private async void CloseButton_Click(object? sender, RoutedEventArgs e)
    {

        if (Settings?.SaveFromSeconds < 30)
        {
            await new CustomMessageBox("Intervalo de gravação", "Tempo mínimo de 30 segundos", "Ok").ShowDialogAsync(this);
            return;
        }

        GshopSettings.Save(Settings);
        this.Close();
    }
}