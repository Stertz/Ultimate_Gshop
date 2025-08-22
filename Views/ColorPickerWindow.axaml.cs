using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Views
{
    public partial class ColorPickerWindow : Window
    {
        private readonly List<Color> _palette =
        [
            // paleta com 50 cores baseada na imagem
            Color.Parse("#FFF1F1"), Color.Parse("#F1E7F8"), Color.Parse("#EAF6F9"), Color.Parse("#ECF7F1"), Color.Parse("#FFF9DC"),
            Color.Parse("#FFF0E6"), Color.Parse("#F5F5F5"), Color.Parse("#E1E3E8"), Color.Parse("#D9D9D9"), Color.Parse("#F2F2F2"),
            Color.Parse("#E6B1AA"), Color.Parse("#D9C3E3"), Color.Parse("#B9E2EA"), Color.Parse("#BFE7CC"), Color.Parse("#F9E09F"),
            Color.Parse("#F8CBA7"), Color.Parse("#E5E5E5"), Color.Parse("#BFC3CC"), Color.Parse("#AAB2BD"), Color.Parse("#C9D1D9"),
            Color.Parse("#DB4D41"), Color.Parse("#AB66CC"), Color.Parse("#50C4D4"), Color.Parse("#58CC8A"), Color.Parse("#FFD43B"),
            Color.Parse("#F79F24"), Color.Parse("#E8F0F7"), Color.Parse("#A9B4C2"), Color.Parse("#7C8796"), Color.Parse("#546E7A"),
            Color.Parse("#C0392B"), Color.Parse("#8E44AD"), Color.Parse("#16A085"), Color.Parse("#27AE60"), Color.Parse("#F1C40F"),
            Color.Parse("#E67E22"), Color.Parse("#7F8C8D"), Color.Parse("#616A6B"), Color.Parse("#34495E"), Color.Parse("#2C3E50"),
            Color.Parse("#7B241C"), Color.Parse("#512E5F"), Color.Parse("#1B4F72"), Color.Parse("#0E6251"), Color.Parse("#196F3D"),
            Color.Parse("#9A7D0A"), Color.Parse("#784212"), Color.Parse("#424949"), Color.Parse("#2E4053"), Color.Parse("#1C2833")
        ];

        public ColorPickerWindow()
        {
            InitializeComponent();
            Cursor = ApplicationSettings.InitializeCustomCursor();
            BuildGrid();

            this.KeyDown += ColorPickerWindow_KeyDown;
        }

        private void BuildGrid()
        {
            foreach (var color in _palette)
            {
                var button = new Button
                {
                    Background = new SolidColorBrush(color),
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(2),
                    Height = 30,
                    Width = 30,
                    Tag = color
                };

                button.DoubleTapped += OnButtonDoubleTapped;
                ColorGrid.Children.Add(button);
            }
        }

        public static async Task<Color?> ShowAsync(Window parent)
        {
            var dialog = new ColorPickerWindow();
            return await dialog.ShowDialog<Color?>(parent);
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.ClickCount == 2 && sender is Button button && button.Tag is Color color)
            {
                Close(color);
            }
        }

        private void OnButtonDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Color color)
            {
                Close(color);
            }
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            this.Close(null);
        }

        private void ColorPickerWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close(null);
            }
        }
    }
}
