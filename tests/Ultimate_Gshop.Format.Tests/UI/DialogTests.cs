using Avalonia.Controls;
using Avalonia.Interactivity;
using Ultimate_Gshop.Views;
using Ultimate_Gshop.Views.UserControls;

namespace Ultimate_Gshop.Format.Tests;

[Collection("Image rendering")]
public sealed class DialogTests(ImageRenderingFixture fixture)
{
    private static void Click(CustomMessageBox dialog, int index = 0) =>
        dialog.FindControl<Panel>("ButtonPanel")!.Children.OfType<Button>().ElementAt(index)
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    [Theory]
    [InlineData(CustomMessageBox.MessageBoxIcon.None)]
    [InlineData(CustomMessageBox.MessageBoxIcon.Information)]
    [InlineData(CustomMessageBox.MessageBoxIcon.Question)]
    [InlineData(CustomMessageBox.MessageBoxIcon.Error)]
    public Task ButtonDialogCompletesWithTheClickedChoiceAndCorrectIcon(CustomMessageBox.MessageBoxIcon icon) => fixture.Run(async () =>
    {
        using var host = new HeadlessHost(new Border());
        var dialog = new CustomMessageBox("Confirm", "Choose an action", icon, "Yes", "No");
        try
        {
            var result = dialog.ShowDialogAsync(host.Window);
            Assert.Equal("Confirm", dialog.FindControl<TextBlock>("TitleBlock")!.Text);
            Assert.Equal("Choose an action", dialog.FindControl<TextBlock>("MessageBlock")!.Text);
            Assert.Equal(icon != CustomMessageBox.MessageBoxIcon.None, dialog.FindControl<Image>("IconImage")!.IsVisible);
            Click(dialog, 1);
            Assert.Equal("No", await result.WaitAsync(TimeSpan.FromSeconds(2)));
        }
        finally { dialog.Close(); }
    });

    [Fact]
    public Task InputDialogReturnsTheEditedTextAndRejectsTheWrongApi() => fixture.Run(async () =>
    {
        using var host = new HeadlessHost(new Border());
        var dialog = new CustomMessageBox("Rename", "New name", CustomMessageBox.MessageBoxIcon.None,
            withInput: true, inputLabel: "Name", initialText: "Before", buttons: ["OK", "Cancel"]);
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => dialog.ShowComboDialogAsync(host.Window));
            var input = dialog.FindControl<FloatingLabelTextBox>("InputBox")!;
            Assert.Equal("Before", input.Text);
            Assert.Equal("Name", input.Label);
            var result = dialog.ShowInputDialogAsync(host.Window);
            input.Text = "After";
            Click(dialog);
            var chosen = await result.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal("OK", chosen.Button);
            Assert.Equal("After", chosen.Text);
        }
        finally { dialog.Close(); }
    });

    [Fact]
    public Task OneComboDialogReturnsTheCurrentSelection() => fixture.Run(async () =>
    {
        using var host = new HeadlessHost(new Border());
        var dialog = new CustomMessageBox("Move", "Category", CustomMessageBox.MessageBoxIcon.None,
            "Category", new[] { "First", "Second" }, "First", "OK");
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => dialog.ShowInputDialogAsync(host.Window));
            var result = dialog.ShowComboDialogAsync(host.Window);
            dialog.FindControl<ComboBox>("ComboBox1")!.SelectedItem = "Second";
            Click(dialog);
            var chosen = await result.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal("Second", chosen.Selected1);
            Assert.Null(chosen.Selected2);
            Assert.False(dialog.FindControl<Panel>("ComboPanel2")!.IsVisible);
        }
        finally { dialog.Close(); }
    });

    [Fact]
    public Task IndependentCombosSupportSourceReplacementAndReturnBothChoices() => fixture.Run(async () =>
    {
        using var host = new HeadlessHost(new Border());
        var dialog = new CustomMessageBox("Move", "Choose", CustomMessageBox.MessageBoxIcon.None,
            "Category", new[] { "A" }, "A", "Subcategory", new[] { "A1" }, "A1", "OK");
        try
        {
            var result = dialog.ShowComboDialogAsync(host.Window);
            dialog.SetCombo1Items(new[] { "B", "C" }, "B");
            dialog.SetCombo2Items(new[] { "B1", "B2" }, "B2");
            Click(dialog);
            var chosen = await result.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal("B", chosen.Selected1);
            Assert.Equal("B2", chosen.Selected2);
        }
        finally { dialog.Close(); }
    });

    [Fact]
    public Task DependentComboRefreshesAndClearsAStaleChildSelection() => fixture.Run(async () =>
    {
        using var host = new HeadlessHost(new Border());
        var dialog = new CustomMessageBox("Move", "Choose", CustomMessageBox.MessageBoxIcon.None,
            "Category", new[] { "A", "B" }, "A", "Subcategory",
            parent => parent is string value ? new[] { value + "1", value + "2" } : Array.Empty<string>(),
            "A2", "OK");
        try
        {
            var result = dialog.ShowComboDialogAsync(host.Window);
            var child = dialog.FindControl<ComboBox>("ComboBox2")!;
            Assert.Equal("A2", child.SelectedItem);
            dialog.SetCombo1Items(new[] { "B", "C" }, "B");
            Assert.Null(child.SelectedItem);
            Assert.Equal(new[] { "B1", "B2" }, child.Items.Cast<string>());
            child.SelectedItem = "B1";
            Click(dialog);
            var chosen = await result.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal("B", chosen.Selected1);
            Assert.Equal("B1", chosen.Selected2);
        }
        finally { dialog.Close(); }
    });

    [Theory]
    [InlineData("buttons")]
    [InlineData("input")]
    [InlineData("combo")]
    public Task ClosingADialogCompletesItsPendingTaskAsCancellation(string mode) => fixture.Run(async () =>
    {
        using var host = new HeadlessHost(new Border());
        var dialog = mode switch
        {
            "input" => new CustomMessageBox("Title", "Message", CustomMessageBox.MessageBoxIcon.None,
                withInput: true, buttons: ["OK", "Cancelar"]),
            "combo" => new CustomMessageBox("Title", "Message", CustomMessageBox.MessageBoxIcon.None,
                "Category", new[] { "A" }, "A", "OK", "Cancelar"),
            _ => new CustomMessageBox("Title", "Message", "Sim", "Não")
        };
        Task result = mode switch
        {
            "input" => dialog.ShowInputDialogAsync(host.Window),
            "combo" => dialog.ShowComboDialogAsync(host.Window),
            _ => dialog.ShowDialogAsync(host.Window)
        };
        dialog.Close();
        Assert.True(result.IsCompletedSuccessfully, "Closing a window must release its awaiting caller.");
        await result;
        var button = mode switch
        {
            "input" => (await (Task<CustomMessageBox.MessageBoxResult>)result).Button,
            "combo" => (await (Task<CustomMessageBox.ComboBoxResult>)result).Button,
            _ => await (Task<string>)result
        };
        Assert.Equal("Cancelar", button);
    });
}
