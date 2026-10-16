using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Ultimate_Gshop.Views.UserControls;

namespace Ultimate_Gshop.Format.Tests;

[Collection("Image rendering")]
public sealed class EditorControlTests(ImageRenderingFixture fixture)
{
    [Theory]
    [InlineData(InputMode.Number, "a-12.3x4", "1234")]
    [InlineData(InputMode.Number, "", "")]
    [InlineData(InputMode.Decimal, " 12.345x6 ", "12,34")]
    [InlineData(InputMode.Decimal, "1,2.3", "1,23")]
    [InlineData(InputMode.Decimal, ".5", ",5")]
    [InlineData(InputMode.String, " free ^text ", " free ^text ")]
    public Task TextEntryFiltersOnlyTheRequestedInputMode(InputMode mode, string input, string expected) => fixture.Run(() =>
    {
        var control = new FloatingLabelTextBox { InputMode = mode, Label = "Price" };
        using var host = new HeadlessHost(control);
        var field = control.FindControl<TextBox>("TextField")!;
        field.Text = input;
        Assert.Equal(expected, field.Text);
        Assert.Equal(expected, control.Text);
        Assert.Equal("Price", control.FindControl<TextBlock>("LabelBlock")!.Text);
    });

    [Fact]
    public Task TextPropertyBindingWorksInBothDirectionsAndFloatsTheLabel() => fixture.Run(() =>
    {
        var control = new FloatingLabelTextBox { Label = "Name", Text = "Initial" };
        using var host = new HeadlessHost(control);
        var field = control.FindControl<TextBox>("TextField")!;
        var label = control.FindControl<TextBlock>("LabelBlock")!;
        Assert.Equal("Initial", field.Text);
        field.Text = "Edited";
        Assert.Equal("Edited", control.Text);
        Assert.Equal(0, Assert.IsType<TranslateTransform>(label.RenderTransform).Y);
        field.Text = "";
        Assert.Equal("", control.Text);
        Assert.Equal(14, Assert.IsType<TranslateTransform>(label.RenderTransform).Y);
    });

    [Fact]
    public Task DatePickerConvertsDatesAndSynchronizesItsInnerPicker() => fixture.Run(() =>
    {
        var control = new StyledDatePicker { LabelText = "Starts", SelectedDate = new DateTime(2024, 2, 29) };
        using var host = new HeadlessHost(control);
        var picker = control.FindControl<DatePicker>("Picker")!;
        var text = control.FindControl<TextBlock>("FormattedDateText")!;
        Assert.Equal(20240229, control.DateAsInt);
        Assert.Equal("29/02/24", text.Text);
        Assert.Equal(new DateTime(2024, 2, 29), picker.SelectedDate?.Date);
        picker.SelectedDate = new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(20251231, control.DateAsInt);
        Assert.Equal("31/12/25", text.Text);
        control.SetValue(StyledDatePicker.SelectedDateProperty, new DateTime(2000, 1, 1));
        Assert.Equal(new DateTime(2000, 1, 1), picker.SelectedDate?.Date);
        Assert.Equal("01/01/00", text.Text);
        control.SelectedDate = null;
        Assert.Null(control.DateAsInt);
        Assert.Null(picker.SelectedDate);
        Assert.Equal("", text.Text);
        control.LabelText = "";
        Assert.False(control.FindControl<TextBlock>("Label")!.IsVisible);
    });

    [Fact]
    public Task ComboBoxBindingsSynchronizeItemsAndSelection() => fixture.Run(() =>
    {
        var control = new StyledComboBox { LabelText = "Category", ItemsSource = new[] { "First", "Second" } };
        using var host = new HeadlessHost(control);
        var combo = control.FindControl<ComboBox>("Combo")!;
        Assert.Equal(2, combo.Items.Count);
        control.SelectedIndex = 1;
        Assert.Equal("Second", combo.SelectedItem);
        Assert.Equal("Second", control.SelectedItem);
        combo.SelectedItem = "First";
        Assert.Equal(0, control.SelectedIndex);
        Assert.Equal("First", control.SelectedItem);
        control.LabelText = "";
        Assert.False(control.FindControl<TextBlock>("Label")!.IsVisible);
    });

    [Theory]
    [InlineData(null, "^ffffff")]
    [InlineData("Plain text", "^ffffffPlain text")]
    [InlineData("Line one\nLine two", "^ffffffLine one\\rLine two")]
    [InlineData("^FF0000Red\\r^00FF00Green", "^ff0000Red\\r^00ff00Green")]
    public Task ExplanationPreviewExportsTaggedTextAndPreservesLineBreaks(string? input, string expected) => fixture.Run(() =>
    {
        var control = new ExplanationEditorControl();
        using var host = new HeadlessHost(control);
        control.ImportTagged(input);
        Assert.Equal(expected, control.ExportText());
        if (input?.StartsWith("^FF0000") == true)
        {
            var runs = control.FindControl<TextBlock>("PreviewTextBlock")!.Inlines!.OfType<Run>().ToList();
            Assert.Equal(2, runs.Count);
            Assert.Equal(Colors.Red, Assert.IsAssignableFrom<ISolidColorBrush>(runs[0].Foreground).Color);
            Assert.Equal("Red\n", runs[0].Text);
        }
    });

    [Fact]
    public Task ExplanationToggleEditsTheDisplayedTextAndHonorsEditingState() => fixture.Run(() =>
    {
        var control = new ExplanationEditorControl();
        using var host = new HeadlessHost(control);
        var editor = control.FindControl<TextBox>("EditorTextBox")!;
        var palette = control.FindControl<Button>("PaletteButton")!;
        Assert.False(editor.IsEnabled);
        control.ImportTagged("^ffffffBefore");
        control.IsEditing = true;
        var toggle = control.GetVisualDescendants().OfType<Button>().Single(button => button != palette);
        toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.False(control.IsPreviewing);
        Assert.True(editor.IsVisible);
        Assert.True(palette.IsVisible);
        Assert.True(editor.IsEnabled);
        editor.Text = "^ABCDEFEdited\nSecond line";
        Assert.Equal("^ABCDEFEdited\\rSecond line", control.ExportText());
        toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.True(control.IsPreviewing);
        Assert.False(editor.IsVisible);
        control.IsEditing = false;
        Assert.False(palette.IsEnabled);
    });
}
