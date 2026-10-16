using Avalonia;
using Avalonia.Controls;
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

        public static readonly StyledProperty<string> LabelTextProperty =
            AvaloniaProperty.Register<StyledComboBox, string>(nameof(LabelText));

        public string LabelText
        {
            get => GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }

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

        private readonly TextBlock? _label;

        public StyledComboBox()
        {
            AvaloniaXamlLoader.Load(this);

            _label = this.FindControl<TextBlock>("Label");

            this.GetObservable(LabelTextProperty).Subscribe(OnLabelTextChanged);
        }

        private void OnLabelTextChanged(string? newValue)
        {
            if (_label != null)
            {
                _label.Text = newValue ?? string.Empty;
                _label.IsVisible = !string.IsNullOrWhiteSpace(newValue);
            }
        }
    }
}
