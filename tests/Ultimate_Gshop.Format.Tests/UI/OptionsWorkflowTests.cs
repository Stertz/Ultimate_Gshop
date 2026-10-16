using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Views;
using Ultimate_Gshop.Views.UserControls;

namespace Ultimate_Gshop.Format.Tests;

[Collection("Image rendering")]
public sealed class OptionsWorkflowTests(ImageRenderingFixture fixture)
{
    private static void Save(OptionsWindow window) => window.GetVisualDescendants().OfType<Button>()
        .Single(button => button.Width == 30 && button.Height == 30).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void Answer(OptionsWindow window) => window.OwnedWindows.OfType<CustomMessageBox>().Single()
        .FindControl<Panel>("ButtonPanel")!.Children.OfType<Button>().First().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static async Task PumpAsync() => await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

    [Fact]
    public Task ClosingWithoutSaveDiscardsDraftPreferences() => fixture.Run(() =>
    {
        using var directory = new TestDirectory();
        var original = new GshopSettings { AutoSave = false, SaveFromSeconds = 60 };
        var window = new OptionsWindow(original, directory.Path);
        window.Show();
        window.FindControl<ToggleButton>("AutoSaveToggleButton")!.IsChecked = true;
        window.FindControl<FloatingLabelTextBox>("SaveFromSecondsTextBox")!.Text = "120";
        Assert.True(window.Settings.AutoSave);
        Assert.False(original.AutoSave);
        window.Close();
        Assert.Equal(60, original.SaveFromSeconds);
        Assert.Empty(Directory.EnumerateFileSystemEntries(directory.Path));
    });

    [Theory]
    [InlineData("90", 90)]
    [InlineData("", null)]
    public Task SavingCommitsTheDraftAndPreservesSessionMetadata(string text, int? interval) => fixture.Run(() =>
    {
        using var directory = new TestDirectory();
        var original = new GshopSettings { SaveFromSeconds = 60, LastBackupFrom = "Backup", LastGshopLoadedFrom = "Source" };
        var window = new OptionsWindow(original, directory.Path);
        try
        {
            window.Show();
            window.FindControl<ToggleButton>("AutoSaveToggleButton")!.IsChecked = true;
            window.FindControl<FloatingLabelTextBox>("SaveFromSecondsTextBox")!.Text = text;
            Save(window);
            Assert.False(window.IsVisible);
            Assert.True(original.AutoSave);
            Assert.Equal(interval, original.SaveFromSeconds);
            var loaded = GshopSettings.Load(Path.Combine(directory.Path, "settings.json"));
            Assert.Equal(interval, loaded.SaveFromSeconds);
            Assert.Equal("Backup", loaded.LastBackupFrom);
            Assert.Equal("Source", loaded.LastGshopLoadedFrom);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData("5")]
    [InlineData("999999999999999999999")]
    public Task InvalidIntervalKeepsTheDraftOpenWithoutChangingLivePreferences(string text) => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var original = new GshopSettings { SaveFromSeconds = 60 };
        var window = new OptionsWindow(original, directory.Path);
        try
        {
            window.Show();
            var field = window.FindControl<FloatingLabelTextBox>("SaveFromSecondsTextBox")!;
            field.Text = text;
            Save(window);
            Save(window);
            Assert.Single(window.OwnedWindows);
            Answer(window);
            await PumpAsync();
            Assert.True(window.IsVisible);
            Assert.Equal(60, original.SaveFromSeconds);
            Assert.False(File.Exists(Path.Combine(directory.Path, "settings.json")));
            field.Text = "120";
            Save(window);
            Assert.False(window.IsVisible);
            Assert.Equal(120, original.SaveFromSeconds);
        }
        finally { window.Close(); }
    });

    [Fact]
    public Task FailedSettingsWritePreservesLivePreferencesAndAllowsRetry() => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var original = new GshopSettings { AutoSave = false, SaveFromSeconds = 60 };
        var blocker = Path.Combine(directory.Path, "settings.json");
        Directory.CreateDirectory(blocker);
        var window = new OptionsWindow(original, directory.Path);
        try
        {
            window.Show();
            window.FindControl<ToggleButton>("AutoSaveToggleButton")!.IsChecked = true;
            window.FindControl<FloatingLabelTextBox>("SaveFromSecondsTextBox")!.Text = "90";
            Save(window);
            Answer(window);
            await PumpAsync();
            Assert.True(window.IsVisible);
            Assert.False(original.AutoSave);
            Assert.Equal(60, original.SaveFromSeconds);
            Directory.Delete(blocker);
            Save(window);
            Assert.False(window.IsVisible);
            Assert.True(original.AutoSave);
            Assert.Equal(90, original.SaveFromSeconds);
        }
        finally { window.Close(); }
    });
}
