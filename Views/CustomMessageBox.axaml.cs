using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Views
{
    public partial class CustomMessageBox : Window
    {
        private readonly TaskCompletionSource<string> _tcsButtonOnly = new();
        public sealed record MessageBoxResult(string Button, string? Text);

        private TaskCompletionSource<MessageBoxResult>? _tcsWithInput;
        private bool _withInput;

        public enum MessageBoxIcon
        {
            None,
            Information,
            Question,
            Error
        }

        public CustomMessageBox()
        {
            InitializeComponent();
            Cursor = ApplicationSettings.InitializeCustomCursor();
        }

        public CustomMessageBox(string title, string message, params string[] buttons)
            : this(title, message, MessageBoxIcon.None, buttons) { }

        public CustomMessageBox(string title, string message, MessageBoxIcon icon, params string[] buttons)
        {
            InitializeComponent();
            Cursor = ApplicationSettings.InitializeCustomCursor();

            TitleBlock.Text = title;
            MessageBlock.Text = message;
            SetIcon(icon);

            BuildButtons(buttons, resultToInput: false);
        }

        public CustomMessageBox(string title, string message, MessageBoxIcon icon,
                                bool withInput, string? inputLabel = null, string? initialText = null,
                                params string[] buttons)
            : this()
        {
            TitleBlock.Text = title;
            MessageBlock.Text = message;
            SetIcon(icon);

            _withInput = withInput;

            if (_withInput)
            {
                InputPanel.IsVisible = true;
                if (!string.IsNullOrWhiteSpace(inputLabel))
                    InputBox.Label = inputLabel;

                if (!string.IsNullOrEmpty(initialText))
                    InputBox.Text = initialText;

                _tcsWithInput = new TaskCompletionSource<MessageBoxResult>();
            }

            BuildButtons(buttons, resultToInput: _withInput);
        }

        private void BuildButtons(string[] buttons, bool resultToInput)
        {
            ButtonPanel.Children.Clear();

            foreach (var btnText in buttons)
            {
                var button = new Button
                {
                    Width = 88,
                    Height = 34,
                    Padding = new Avalonia.Thickness(0),
                    Margin = new Avalonia.Thickness(0),
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
                    if (resultToInput && _tcsWithInput is not null)
                    {
                        var text = InputBox?.Text;
                        _tcsWithInput.TrySetResult(new MessageBoxResult(btnText, text));
                    }
                    else
                    {
                        _tcsButtonOnly.TrySetResult(btnText);
                    }
                    Close();
                };

                ButtonPanel.Children.Add(button);
            }
        }

        private void SetIcon(MessageBoxIcon icon)
        {
            string? path = icon switch
            {
                MessageBoxIcon.Information => "avares://Ultimate_Gshop/Resources/information.png",
                MessageBoxIcon.Question => "avares://Ultimate_Gshop/Resources/question.png",
                MessageBoxIcon.Error => "avares://Ultimate_Gshop/Resources/error.png",
                _ => null
            };

            if (path is not null)
            {
                IconImage.Source = new Bitmap(AssetLoader.Open(new Uri(path)));
                IconImage.IsVisible = true;
            }
            else
            {
                IconImage.IsVisible = false;
            }
        }
        public Task<string> ShowDialogAsync(Window owner)
        {
            base.ShowDialog(owner);
            return _tcsButtonOnly.Task;
        }

        public Task<MessageBoxResult> ShowInputDialogAsync(Window owner)
        {
            if (!_withInput || _tcsWithInput is null)
                throw new InvalidOperationException("Este diálogo não foi criado no modo com input.");

            base.ShowDialog(owner);
            return _tcsWithInput.Task;
        }
    }
}
