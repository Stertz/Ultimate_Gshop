using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

namespace Ultimate_Gshop.Views.UserControls
{
    public enum InputMode
    {
        String,   // padrão
        Number,   // inteiro somente
        Decimal   // decimal com 2 casas
    }

    public partial class FloatingLabelTextBox : UserControl
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<FloatingLabelTextBox, string>(nameof(Label));

        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<FloatingLabelTextBox, string>(nameof(Text));

        public static readonly StyledProperty<InputMode> InputModeProperty =
            AvaloniaProperty.Register<FloatingLabelTextBox, InputMode>(
                nameof(InputMode), defaultValue: InputMode.String);

        // ajuste aqui se quiser outra quantidade de casas decimais
        private const int DecimalPlaces = 2;

        private const string INT_REGEX_PATTERN = @"^\d*$";
        private const string DEC_REGEX_PATTERN = @"^\d*([.,]\d{0,2})?$";

        [GeneratedRegex(INT_REGEX_PATTERN, RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
        private static partial Regex IntRegex();
        [GeneratedRegex(DEC_REGEX_PATTERN, RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
        private static partial Regex DecRegex();

        private bool _coercing;

        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public InputMode InputMode
        {
            get => GetValue(InputModeProperty);
            set => SetValue(InputModeProperty, value);
        }

        public FloatingLabelTextBox()
        {
            InitializeComponent();

            // Animação da label flutuante
            TextField.GotFocus += (_, _) => AnimateLabel(true);
            TextField.LostFocus += (_, _) =>
            {
                Sanitize(); // limpa ao sair
                AnimateLabel(!string.IsNullOrEmpty(Text));
            };

            TextField.PropertyChanged += (_, e) =>
            {
                if (e.Property == TextBox.TextProperty)
                {
                    CoerceCurrentText(); // bloqueia imediatamente
                    AnimateLabel(!string.IsNullOrEmpty(Text));
                }
            };

            // Colagem (normaliza/filtra)
            TextField.PastingFromClipboard += OnPaste;

            // Estado inicial da label
            AnimateLabel(!string.IsNullOrEmpty(Text));
        }

        private void AnimateLabel(bool floatUp)
        {
            if (LabelBlock.RenderTransform is TranslateTransform transform)
            {
                transform.Y = floatUp ? 0 : 14;
                LabelBlock.FontSize = floatUp ? 10 : 12;
                LabelBlock.Foreground = floatUp ? Brushes.White : Brushes.Gray;
                LabelBlock.FontWeight = floatUp ? FontWeight.Bold : FontWeight.Normal;
            }
        }

        private void CoerceCurrentText()
        {
            if (_coercing || InputMode == InputMode.String) return;

            var current = TextField.Text ?? string.Empty;
            var filtered = Filter(current);

            if (filtered == current) return;

            var caret = TextField.CaretIndex;

            _coercing = true;
            TextField.Text = filtered;
            TextField.CaretIndex = Math.Min(filtered.Length, caret);
            _coercing = false;

            Text = filtered; // sincroniza propriedade pública
        }

        private async void OnPaste(object? sender, RoutedEventArgs e)
        {
            var top = TopLevel.GetTopLevel(this) ?? TopLevel.GetTopLevel(TextField);
            var clipboard = top?.Clipboard;
            if (clipboard is null) return;

            var text = await clipboard.GetTextAsync();
            if (string.IsNullOrEmpty(text)) return;

            var filtered = Filter(text);
            if (filtered != text)
            {
                var selStart = TextField.SelectionStart;
                var selEnd = selStart + TextField.SelectionEnd - TextField.SelectionStart;

                TextField.Text = (TextField.Text ?? string.Empty)
                                .Remove(selStart, selEnd - selStart)
                                .Insert(selStart, filtered);

                TextField.CaretIndex = selStart + filtered.Length;
                e.Handled = true;
            }
        }

        private string Filter(string input)
        {
            var s = input.Trim();

            if (InputMode == InputMode.Number)
            {
                // mantém apenas dígitos
                return string.Concat(s.Where(char.IsDigit));
            }

            if (InputMode == InputMode.Decimal)
            {
                // mantém dígitos e um único separador (vírgula ou ponto)
                var buf = new List<char>();
                bool sep = false;
                foreach (var c in s)
                {
                    if (char.IsDigit(c)) buf.Add(c);
                    else if ((c == '.' || c == ',') && !sep) { buf.Add(','); sep = true; }
                }

                var r = new string([.. buf]);
                // limita casas decimais
                var idx = r.IndexOf(',');
                if (idx >= 0 && r.Length > idx + 1 + DecimalPlaces)
                    r = r[..(idx + 1 + DecimalPlaces)];

                return r;
            }

            // String: sem filtro
            return s;
        }

        private void Sanitize()
        {
            var t = TextField.Text ?? "";

            switch (InputMode)
            {
                case InputMode.Number:
                    if (!IntRegex().IsMatch(t))
                        TextField.Text = Filter(t);
                    break;

                case InputMode.Decimal:
                    if (!DecRegex().IsMatch(t))
                        TextField.Text = Filter(t);

                    TextField.Text = TextField.Text?.Replace(',', '.');
                    break;

                case InputMode.String:
                default:
                    break;
            }

            // sincroniza propriedade Text do controle
            Text = TextField.Text ?? "";
        }
    }
}
