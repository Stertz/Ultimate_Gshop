using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Ultimate_Gshop
{
    public partial class GameMessageBox : Window
    {
        private readonly TaskCompletionSource<string> _tcs = new();

        public GameMessageBox()
        {
            InitializeComponent();
        }

        public GameMessageBox(string title, string message, params string[] buttons)
        {
            InitializeComponent();

            TitleBlock.Text = title;
            MessageBlock.Text = message;

            foreach (var btnText in buttons)
            {
                var button = new Button
                {
                    Width = 100,
                    Height = 40,
                    Background = Avalonia.Media.Brushes.Transparent,
                    BorderBrush = Avalonia.Media.Brushes.Transparent
                };

                var grid = new Grid();

                var bgImage = new Image
                {
                    Source = new Bitmap(
                        AssetLoader.Open(new Uri("avares://Ultimate_Gshop/Resources/btn.png"))
                    ),
                    Stretch = Avalonia.Media.Stretch.Fill
                };

                var label = new TextBlock
                {
                    Text = btnText,
                    Foreground = Avalonia.Media.Brushes.White,
                    FontSize = 12,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                grid.Children.Add(bgImage);
                grid.Children.Add(label);

                button.Content = grid;

                button.Click += (_, __) =>
                {
                    _tcs.TrySetResult(btnText);
                    Close();
                };

                ButtonPanel.Children.Add(button);
            }
        }

        public Task<string> ShowDialogAsync(Window owner)
        {
            base.ShowDialog(owner);
            return _tcs.Task;
        }
    }
}
