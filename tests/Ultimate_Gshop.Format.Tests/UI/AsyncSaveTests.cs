using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Views;

namespace Ultimate_Gshop.Format.Tests;

[Collection("Image rendering")]
public sealed class AsyncSaveTests(ImageRenderingFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task CloseWaitsForAutosaveAndPersistsEditsMadeAfterItsSnapshot(bool failFirst) => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = ShopFixture.Create(1);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writes = 0;
        var window = new MainWindow(shop, new ShopTracking(), new GshopSettings(), directory.Path, async (snapshot, root) =>
        {
            if (++writes == 1)
            {
                await release.Task;
                if (failFirst) throw new IOException("Controlled disk failure");
            }
            await snapshot.WriteAsync(root);
        });
        window.Show();
        try
        {
            var save = window.SaveSessionAsync();
            Assert.False(save.IsCompleted);
            // The dispatcher must deliver input work while disk I/O is outstanding.
            await Dispatcher.UIThread.InvokeAsync(() => shop.Itens[0].Name = "Latest edit");
            Assert.False(await window.SaveSessionAsync());
            window.Close();
            window.Close();
            Assert.True(window.IsVisible);
            Assert.False(window.IsEnabled);
            Assert.Equal(1, writes);
            release.SetResult();
            if (failFirst)
            {
                await HeadlessWait.UntilAsync(() => window.OwnedWindows.OfType<CustomMessageBox>().Any());
                window.OwnedWindows.OfType<CustomMessageBox>().Single().FindControl<Panel>("ButtonPanel")!
                    .Children.OfType<Button>().Single().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.False(await save);
                await HeadlessWait.UntilAsync(() => window.IsEnabled);
                Assert.True(window.IsVisible);
                Assert.Equal(1, writes);
                window.Close();
            }
            else Assert.True(await save);
            await HeadlessWait.UntilAsync(() => !window.IsVisible);
            Assert.Equal(2, writes);
            Assert.Equal("Latest edit", Assert.Single(ContinueGshop.Handle(directory.Path).Itens).Name);
        }
        finally
        {
            release.TrySetResult();
            foreach (var dialog in window.OwnedWindows.ToArray()) dialog.Close();
            if (window.IsVisible) { window.Close(); await HeadlessWait.UntilAsync(() => !window.IsVisible); }
        }
    });

    [Fact]
    public Task AutosaveWritesCapturedGenerationAndMenuWaitsForIt() => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = ShopFixture.Create(1);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var window = new MainWindow(shop, new ShopTracking(), new GshopSettings(), directory.Path, async (snapshot, root) =>
        {
            await release.Task;
            await snapshot.WriteAsync(root);
        });
        window.Show();
        try
        {
            var before = ShopFixture.Export(shop);
            var save = window.SaveSessionAsync();
            shop.Itens[0].Name = "Later edit";
            // Invoke the real navigation handler while the writer is outstanding.
            var nav = window.FindControl<Button>("NavButton");
            Assert.NotNull(nav);
            nav.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Empty(window.OwnedWindows);
            Assert.False(window.IsEnabled);
            release.SetResult();
            Assert.True(await save);
            await HeadlessWait.UntilAsync(() => window.OwnedWindows.OfType<MenuWindow>().Any());
            Assert.Equal(before, File.ReadAllBytes(Path.Combine(directory.Path, "gshop_temp.data")));
            Assert.False(await window.SaveSessionAsync());
            window.Close();
            Assert.True(window.IsVisible);
            window.OwnedWindows.OfType<MenuWindow>().Single().Close();
            await HeadlessWait.UntilAsync(() => window.IsEnabled);
        }
        finally
        {
            release.TrySetResult();
            foreach (var dialog in window.OwnedWindows.ToArray()) dialog.Close();
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
            window.Close();
            await HeadlessWait.UntilAsync(() => !window.IsVisible);
        }
        Assert.Equal("Later edit", Assert.Single(ContinueGshop.Handle(directory.Path).Itens).Name);
    });
}
