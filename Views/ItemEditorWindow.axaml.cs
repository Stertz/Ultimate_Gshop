using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Ultimate_Gshop.Itens;
using Ultimate_Gshop.Misc;
namespace Ultimate_Gshop
{
    public partial class ItemEditorWindow : Window
    {
        private readonly Item? _item;

        public ItemEditorWindow()
        {
            InitializeComponent();
            Cursor = ApplicationSettings.InitializeCustomCursor();
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
            Cursor = ApplicationSettings.InitializeCustomCursor();
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
                this.Close(_item);
            }
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            this.Close(_item);
        }
    }
}
