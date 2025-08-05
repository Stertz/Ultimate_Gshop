using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Ultimate_Gshop.Itens;
namespace Ultimate_Gshop
{
    public partial class ItemEditorWindow : Window
    {
        private readonly Item? _item;

        public ItemEditorWindow()
        {
            InitializeComponent();
            _item = null;

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

        public ItemEditorWindow(Item item)
        {
            InitializeComponent();
            _item = item;
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

        private void ItemEditorWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
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
