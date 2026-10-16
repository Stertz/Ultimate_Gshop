using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Ultimate_Gshop.Misc;
using Ultimate_Gshop.Views.UserControls;

namespace Ultimate_Gshop.Format.Tests;

[Collection("Image rendering")]
public sealed class VirtualizedItemsTests(ImageRenderingFixture fixture)
{
    [Fact]
    public Task LargeListsLoadOnlyRealizedImagesAndPrepareRecycledDropTargets() => fixture.Run(async () =>
    {
        var resourceRoot = Path.Combine(AppContext.BaseDirectory, "Resources");
        var icons = Directory.EnumerateFiles(Path.Combine(resourceRoot, "surfaces"), "*.png", SearchOption.AllDirectories)
            .Take(1000).Select(path => Path.GetRelativePath(resourceRoot, path)).ToArray();
        var shop = ShopFixture.Create(1000);
        for (var index = 0; index < shop.Itens.Count; index++) shop.Itens[index].Icon = icons[index];
        var before = ImageUtils.CacheEntryCounts;
        var control = new ItemsListControl { ItemsSource = shop.Itens };
        // Projection before attaching must not decode off-screen icons.
        Assert.Equal(before, ImageUtils.CacheEntryCounts);
        using var host = new HeadlessHost(control);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        host.Window.UpdateLayout();
        var cards = control.GetVisualDescendants().OfType<Border>().Where(border => border.Name == "CardBorder").ToArray();
        Assert.InRange(cards.Length, 1, 100);
        Assert.InRange(ImageUtils.CacheEntryCounts.Originals - before.Originals, 0, 100);
        Assert.InRange(ImageUtils.CacheEntryCounts.Reduced - before.Reduced, 0, 100);
        control.ScrollToEnd();
        host.Window.UpdateLayout();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        var last = control.GetVisualDescendants().OfType<Grid>().Single(grid => ReferenceEquals(grid.Tag, shop.Itens[^1]));
        Assert.True(DragDrop.GetAllowDrop(last.GetVisualAncestors().OfType<Border>().First(border => border.Name == "CardBorder")));
        Assert.InRange(control.GetVisualDescendants().OfType<Border>().Count(border => border.Name == "CardBorder"), 1, 100);
    });
}
