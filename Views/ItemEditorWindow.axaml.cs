using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ultimate_Gshop
{
    public partial class ItemEditorWindow : Window
    {
        public ItemEditorWindow()
        {
            InitializeComponent();
            this.Opacity = 0;

            this.KeyDown += ItemEditorWindow_KeyDown;

            this.Opened += async (_, __) =>
            {
                for (double i = 0; i <= 1; i += 0.05)
                {
                    this.Opacity = i;
                    await Task.Delay(7);
                }
                this.Opacity = 1;
            };
        }

        private void ItemEditorWindow_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                this.Close();
            }
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
