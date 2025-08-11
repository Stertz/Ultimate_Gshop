using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Ultimate_Gshop.Views.UserControls
{
    public partial class ExplanationEditorControl : UserControl
    {
        // Texto persistido (tag ^RRGGBB + "\r")
        public static readonly StyledProperty<string?> TextProperty =
            AvaloniaProperty.Register<ExplanationEditorControl, string?>(nameof(Text), "");

        // Habilitação de edição (MainWindow controla)
        public static readonly StyledProperty<bool> IsEditingProperty =
            AvaloniaProperty.Register<ExplanationEditorControl, bool>(nameof(IsEditing), false);

        // Estado de visualização (Preview x Editor) — controlado pelo botão
        public static readonly StyledProperty<bool> IsPreviewingProperty =
            AvaloniaProperty.Register<ExplanationEditorControl, bool>(nameof(IsPreviewing), true);

        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public bool IsEditing
        {
            get => GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }

        public bool IsPreviewing
        {
            get => GetValue(IsPreviewingProperty);
            set => SetValue(IsPreviewingProperty, value);
        }

        [GeneratedRegex(@"\^(?<color>[0-9A-Fa-f]{6})(?<content>.*?)(?=(\^[0-9A-Fa-f]{6})|$)", RegexOptions.Singleline)]
        private static partial Regex ColorTagRegex();

        public ExplanationEditorControl()
        {
            InitializeComponent();

            // Atualiza preview quando Text muda
            this.GetObservable(TextProperty).Subscribe(v =>
            {
                ApplyTextToPreview(v);
                if (!IsPreviewing) // se estou vendo o editor, sincronizo o editor com os runs
                    EditorTextBox.Text = BuildTaggedFromRuns();
            });

            // Apenas habilitação (não mexe na visibilidade)
            this.GetObservable(IsEditingProperty).Subscribe(ApplyEditingState);

            // Troca de modo de visualização
            this.GetObservable(IsPreviewingProperty).Subscribe(ApplyPreviewState);

            this.AttachedToVisualTree += (_, __) =>
            {
                ApplyTextToPreview(Text);
                ApplyEditingState(IsEditing);
                ApplyPreviewState(IsPreviewing);
            };
        }

        // Botão sempre ativo: só alterna Preview x Editor
        private void OnTogglePreviewClick(object? sender, RoutedEventArgs e)
        {
            if (IsPreviewing)
            {
                // indo para o Editor: carrega o editor a partir dos runs atuais
                EditorTextBox.Text = BuildTaggedFromRuns();
                IsPreviewing = false;
            }
            else
            {
                // voltando para o Preview: normaliza o texto do editor e aplica ao preview
                var raw = (EditorTextBox.Text ?? "").Replace("\n", @"\r");
                Text = EnsureStartsWithColor(raw); // dispara ApplyTextToPreview
                IsPreviewing = true;
            }
        }

        private async void OnPaletteClick(object? sender, RoutedEventArgs e)
        {
            if (!EditorTextBox.IsEnabled) return; // só pinta se edição habilitada
            if (this.VisualRoot is not Window win) return;

            EditorTextBox.Text ??= string.Empty;

            var color = await ColorPickerWindow.ShowAsync(win);
            if (!color.HasValue) return;

            var hex = color.Value.ToUInt32().ToString("x8")[2..]; // RRGGBB
            var caret = EditorTextBox.CaretIndex;
            var selected = EditorTextBox.SelectedText ?? string.Empty;
            var s = EditorTextBox.SelectionStart;
            var eidx = EditorTextBox.SelectionEnd;

            if (string.IsNullOrEmpty(selected))
            {
                EditorTextBox.Text = EditorTextBox.Text.Insert(caret, $"^{hex}");
                EditorTextBox.CaretIndex = caret + hex.Length + 1;
            }
            else
            {
                EditorTextBox.Text = EditorTextBox.Text.Remove(s, eidx - s)
                                                       .Insert(s, $"^{hex}{selected}");
                EditorTextBox.CaretIndex = s + hex.Length + 1 + selected.Length;
            }
        }

        // ---- Estados ----
        // Só habilitação; visibilidade é responsabilidade do IsPreviewing
        private void ApplyEditingState(bool editing)
        {
            EditorTextBox.IsEnabled = editing;
            PaletteButton.IsEnabled = editing;
            PaletteButton.Opacity = editing ? 0.9 : 0.25;
        }

        // Controla qual visão aparece e ícone do botão
        private void ApplyPreviewState(bool previewing)
        {
            PreviewHost.IsVisible = previewing;
            EditorTextBox.IsVisible = !previewing;
            PaletteButton.IsVisible = !previewing;

            var icon = previewing
                ? new Uri("avares://Ultimate_Gshop/Resources/preview.png")
                : new Uri("avares://Ultimate_Gshop/Resources/unpreview.png");
            ToggleIcon.Source = new Bitmap(AssetLoader.Open(icon));
        }

        // ---- Renderização do preview ----
        private void ApplyTextToPreview(string? text)
        {
            var inlines = PreviewTextBlock.Inlines;
            if (string.IsNullOrEmpty(text))
            {
                inlines?.Clear();
                return;
            }

            text = EnsureStartsWithColor(text);
            inlines?.Clear();

            foreach (Match match in ColorTagRegex().Matches(text))
            {
                var hex = match.Groups["color"].Value;
                var content = match.Groups["content"].Value.Replace(@"\r", "\n");

                inlines?.Add(new Run
                {
                    Text = content,
                    Foreground = new SolidColorBrush(Color.Parse("#" + hex))
                });
            }
        }

        private string BuildTaggedFromRuns()
        {
            var inlines = PreviewTextBlock.Inlines;
            if (inlines == null || inlines.Count == 0) return "^ffffff";

            var sb = new StringBuilder();
            foreach (var inline in inlines)
            {
                if (inline is Run run)
                {
                    var hex = run.Foreground is ISolidColorBrush brush
                        ? brush.Color.ToUInt32().ToString("x8")[2..]
                        : "ffffff";
                    sb.Append($"^{hex}{run.Text ?? string.Empty}");
                }
            }
            return sb.ToString();
        }

        private static string EnsureStartsWithColor(string? s)
        {
            var txt = s ?? string.Empty;
            if (!txt.StartsWith('^') || txt.Length < 7 ||
                !ColorTagRegex().IsMatch(txt.AsSpan(0, Math.Min(8, txt.Length))))
            {
                txt = "^ffffff" + txt;
            }
            return txt;
        }

        // ---- API pública ----
        public string ExportText()
        {
            // Exporta do que o usuário está vendo/ajustando agora
            var raw = EditorTextBox.IsVisible
                ? (EditorTextBox.Text ?? "")
                : BuildTaggedFromRuns();

            var explanationText = EnsureStartsWithColor(raw.Replace("\n", @"\r"));
            Text = explanationText;
            return explanationText;
        }

        public void ImportTagged(string? tagged)
        {
            var normalized = EnsureStartsWithColor((tagged ?? string.Empty).Replace("\n", @"\r"));
            Text = normalized;            // dispara preview
            if (!IsPreviewing)            // se estiver vendo o editor, sincroniza o editor
                EditorTextBox.Text = BuildTaggedFromRuns();
        }
    }
}
