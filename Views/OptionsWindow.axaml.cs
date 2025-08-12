using Avalonia.Controls;
using Avalonia.Interactivity;
namespace Ultimate_Gshop.Views;

public partial class OptionsWindow : Window
{
    public OptionsWindow()
    {
        InitializeComponent();
    }

    public bool AutoSave { get; set; }
    public int? SaveFromSeconds { get; set; }

    private void AutoSaveToggle_Checked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        AutoSave = true;
    }

    private void AutoSaveToggle_Unchecked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        AutoSave = false;
    }


    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}