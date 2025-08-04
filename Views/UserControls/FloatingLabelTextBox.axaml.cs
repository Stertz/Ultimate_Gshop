using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.TextInput;
using Avalonia.Media;

namespace Ultimate_Gshop.Views.UserControls
{
    public partial class FloatingLabelTextBox : UserControl
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<FloatingLabelTextBox, string>(nameof(Label));

        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<FloatingLabelTextBox, string>(nameof(Text));

        public static readonly StyledProperty<TextInputContentType> ContentTypeProperty =
            AvaloniaProperty.Register<FloatingLabelTextBox, TextInputContentType>(nameof(ContentType));

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

        public TextInputContentType ContentType
        {
            get => GetValue(ContentTypeProperty);
            set => SetValue(ContentTypeProperty, value);
        }

        public FloatingLabelTextBox()
        {
            InitializeComponent();

            // Animação da label flutuante
            TextField.GotFocus += (_, _) => AnimateLabel(true);
            TextField.LostFocus += (_, _) => AnimateLabel(!string.IsNullOrEmpty(Text));
            TextField.PropertyChanged += (_, e) =>
            {
                if (e.Property == TextBox.TextProperty)
                    AnimateLabel(!string.IsNullOrEmpty(Text));
            };

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
    }
}
