using Avalonia.Controls;
using Avalonia.Interactivity;
namespace Ultimate_Gshop;
public partial class TextInputDialog : Window
{
    public string? InputText { get; set; }
    public string Prompt { get; set; } = "Enter value:";
    public new string Title { get; set; } = "Input";

    public TextInputDialog()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        Close(InputText);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}