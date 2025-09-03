using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Ultimate_Gshop.Itens;
using Ultimate_Gshop.ViewModels;

namespace Ultimate_Gshop;

public partial class GridWindow : Window
{
    public GridWindow()
    {
        InitializeComponent();
    }
    public GridWindow(IEnumerable<Item> itens)
    {
        InitializeComponent();
        DataContext = new GridViewModel(itens);
    }
}