using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Views
{
    public partial class CustomMessageBox : Window
    {
        // ---------------- Result Types ----------------
        private readonly TaskCompletionSource<string> _tcsButtonOnly = new();
        public sealed record MessageBoxResult(string Button, string? Text);
        public sealed record ComboBoxResult(string Button, object? Selected1, object? Selected2);

        // ---------------- Internal State ----------------
        private TaskCompletionSource<MessageBoxResult>? _tcsWithInput;
        private TaskCompletionSource<ComboBoxResult>? _tcsWithCombo;

        private enum DialogMode { ButtonsOnly, Input, Combo }
        private DialogMode _mode = DialogMode.ButtonsOnly;

        // Modo combo
        private bool _hasSecondCombo;
        private Func<object?, System.Collections.IEnumerable>? _combo2Resolver; // resolver cascata

        // ---------------- Icon Enum ----------------
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

        // ---------------- Buttons only ----------------
        public CustomMessageBox(string title, string message, params string[] buttons)
            : this(title, message, MessageBoxIcon.None, buttons) { }

        public CustomMessageBox(string title, string message, MessageBoxIcon icon, params string[] buttons)
        {
            InitializeComponent();
            Cursor = ApplicationSettings.InitializeCustomCursor();

            TitleBlock.Text = title;
            MessageBlock.Text = message;
            SetIcon(icon);

            _mode = DialogMode.ButtonsOnly;
            BuildButtons(buttons);
        }

        // ---------------- Input mode ----------------
        public CustomMessageBox(string title, string message, MessageBoxIcon icon,
                                bool withInput, string? inputLabel = null, string? initialText = null,
                                params string[] buttons)
            : this()
        {
            TitleBlock.Text = title;
            MessageBlock.Text = message;
            SetIcon(icon);

            if (withInput)
            {
                _mode = DialogMode.Input;
                InputPanel.IsVisible = true;
                ComboPanel1.IsVisible = false;
                ComboPanel2.IsVisible = false;

                if (!string.IsNullOrWhiteSpace(inputLabel))
                    InputBox.Label = inputLabel;

                if (!string.IsNullOrEmpty(initialText))
                    InputBox.Text = initialText;

                _tcsWithInput = new TaskCompletionSource<MessageBoxResult>();
            }
            else
            {
                _mode = DialogMode.ButtonsOnly;
            }

            BuildButtons(buttons);
        }

        // ---------------- Combo mode (1 combo) ----------------
        public CustomMessageBox(string title, string message, MessageBoxIcon icon,
                                string combo1Label, System.Collections.IEnumerable combo1Items, object? selected1,
                                params string[] buttons)
            : this()
        {
            TitleBlock.Text = title;
            MessageBlock.Text = message;
            SetIcon(icon);

            ConfigureOneCombo(combo1Label, combo1Items, selected1);
            BuildButtons(buttons);
        }

        // ---------------- Combo mode (2 combos independentes) ----------------
        public CustomMessageBox(string title, string message, MessageBoxIcon icon,
                                string combo1Label, System.Collections.IEnumerable combo1Items, object? selected1,
                                string combo2Label, System.Collections.IEnumerable combo2Items, object? selected2,
                                params string[] buttons)
            : this()
        {
            TitleBlock.Text = title;
            MessageBlock.Text = message;
            SetIcon(icon);

            ConfigureTwoCombos(combo1Label, combo1Items, selected1,
                               combo2Label, combo2Items, selected2);
            BuildButtons(buttons);
        }

        // ---------------- Combo mode (2 combos em cascata) ----------------
        public CustomMessageBox(string title, string message, MessageBoxIcon icon,
                                string combo1Label, System.Collections.IEnumerable combo1Items, object? selected1,
                                string combo2Label, Func<object?, System.Collections.IEnumerable> combo2Resolver, object? selected2,
                                params string[] buttons)
            : this()
        {
            TitleBlock.Text = title;
            MessageBlock.Text = message;
            SetIcon(icon);

            _combo2Resolver = combo2Resolver;

            ConfigureTwoCombosDependent(combo1Label, combo1Items, selected1,
                                        combo2Label, selected2);

            BuildButtons(buttons);
        }

        #region ComboBox helpers
        private void ConfigureOneCombo(string label, System.Collections.IEnumerable items, object? selected)
        {
            _mode = DialogMode.Combo;

            InputPanel.IsVisible = false;

            ComboPanel1.IsVisible = true;
            Combo1Label.Text = label;
            ComboBox1.ItemsSource = items;
            ComboBox1.SelectedItem = selected;

            _hasSecondCombo = false;
            ComboPanel2.IsVisible = false;

            _tcsWithCombo = new TaskCompletionSource<ComboBoxResult>();
        }

        private void ConfigureTwoCombos(string label1, System.Collections.IEnumerable items1, object? selected1,
                                        string label2, System.Collections.IEnumerable items2, object? selected2)
        {
            _mode = DialogMode.Combo;

            InputPanel.IsVisible = false;

            // Combo 1
            ComboPanel1.IsVisible = true;
            Combo1Label.Text = label1;
            ComboBox1.ItemsSource = items1;
            ComboBox1.SelectedItem = selected1;

            // Combo 2 (independente)
            _hasSecondCombo = true;
            ComboPanel2.IsVisible = true;
            Combo2Label.Text = label2;
            ComboBox2.ItemsSource = items2;
            ComboBox2.SelectedItem = selected2;

            _tcsWithCombo = new TaskCompletionSource<ComboBoxResult>();
        }

        private void ConfigureTwoCombosDependent(string label1, System.Collections.IEnumerable items1, object? selected1,
                                                 string label2, object? selected2)
        {
            _mode = DialogMode.Combo;

            InputPanel.IsVisible = false;

            // Combo 1
            ComboPanel1.IsVisible = true;
            Combo1Label.Text = label1;
            ComboBox1.ItemsSource = items1;
            ComboBox1.SelectedItem = selected1;

            // Combo 2 (cascata)
            _hasSecondCombo = true;
            ComboPanel2.IsVisible = true;
            Combo2Label.Text = label2;

            RefreshSecondCombo(ComboBox1.SelectedItem, selected2);

            ComboBox1.SelectionChanged += (_, __) =>
            {
                RefreshSecondCombo(ComboBox1.SelectedItem, null);
            };

            _tcsWithCombo = new TaskCompletionSource<ComboBoxResult>();
        }

        private void RefreshSecondCombo(object? firstSelected, object? preferredSelected)
        {
            if (_combo2Resolver is null)
            {
                ComboBox2.ItemsSource = null;
                ComboBox2.SelectedItem = null;
                return;
            }

            var items2 = _combo2Resolver(firstSelected);
            ComboBox2.ItemsSource = items2;
            ComboBox2.SelectedItem = preferredSelected;
        }

        // Helpers opcionais para recarregar externamente
        public void SetCombo1Items(System.Collections.IEnumerable items, object? selected = null)
        {
            ComboBox1.ItemsSource = items;
            ComboBox1.SelectedItem = selected;
            if (_combo2Resolver != null)
                RefreshSecondCombo(ComboBox1.SelectedItem, null);
        }

        public void SetCombo2Items(System.Collections.IEnumerable items, object? selected = null)
        {
            ComboBox2.ItemsSource = items;
            ComboBox2.SelectedItem = selected;
        }
        #endregion

        #region Buttons Builder
        private void BuildButtons(string[] buttons)
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
                    switch (_mode)
                    {
                        case DialogMode.Input:
                            if (_tcsWithInput is not null)
                            {
                                var text = InputBox?.Text;
                                _tcsWithInput.TrySetResult(new MessageBoxResult(btnText, text));
                            }
                            break;

                        case DialogMode.Combo:
                            if (_tcsWithCombo is not null)
                            {
                                var sel1 = ComboBox1?.SelectedItem;
                                var sel2 = _hasSecondCombo ? ComboBox2?.SelectedItem : null;
                                _tcsWithCombo.TrySetResult(new ComboBoxResult(btnText, sel1, sel2));
                            }
                            break;

                        default:
                            _tcsButtonOnly.TrySetResult(btnText);
                            break;
                    }

                    Close();
                };

                ButtonPanel.Children.Add(button);
            }
        }
        #endregion

        #region Icon
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
        #endregion

        #region Public APIs
        public Task<string> ShowDialogAsync(Window owner)
        {
            base.ShowDialog(owner);
            return _tcsButtonOnly.Task;
        }

        public Task<MessageBoxResult> ShowInputDialogAsync(Window owner)
        {
            if (_mode != DialogMode.Input || _tcsWithInput is null)
                throw new InvalidOperationException("Este diálogo não foi criado no modo com input.");

            base.ShowDialog(owner);
            return _tcsWithInput.Task;
        }

        public Task<ComboBoxResult> ShowComboDialogAsync(Window owner)
        {
            if (_mode != DialogMode.Combo || _tcsWithCombo is null)
                throw new InvalidOperationException("Este diálogo não foi criado no modo com ComboBox.");

            base.ShowDialog(owner);
            return _tcsWithCombo.Task;
        }
        #endregion
    }
}
