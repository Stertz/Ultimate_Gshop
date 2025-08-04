using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Ultimate_Gshop.Views.UserControls
{
    public partial class StyledComboBox : UserControl
    {
        public static readonly StyledProperty<object?> ItemsSourceProperty =
            AvaloniaProperty.Register<StyledComboBox, object?>(nameof(ItemsSource));

        public static readonly StyledProperty<int> SelectedIndexProperty =
            AvaloniaProperty.Register<StyledComboBox, int>(nameof(SelectedIndex));

        public static readonly StyledProperty<object?> SelectedItemProperty =
            AvaloniaProperty.Register<StyledComboBox, object?>(nameof(SelectedItem));

        public object? ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public int SelectedIndex
        {
            get => GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        public object? SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public StyledComboBox()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
