using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Views;
using Ultimate_Gshop.Views.UserControls;

namespace Ultimate_Gshop.Format.Tests;

[Collection("Image rendering")]
public sealed class CategoryWorkflowTests(ImageRenderingFixture fixture)
{
    private static void Command(MainWindow window, string title)
    {
        window.UpdateLayout();
        var command = window.GetVisualDescendants().OfType<Grid>()
            .Where(grid => grid.ContextMenu is not null).SelectMany(grid => grid.ContextMenu!.Items.OfType<MenuItem>())
            .First(item => Equals(item.Header, title));
        command.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
    }

    private static CustomMessageBox Dialog(MainWindow window) => window.OwnedWindows.OfType<CustomMessageBox>().Single();
    private static async Task Answer(MainWindow window, int index = 0)
    {
        Dialog(window).FindControl<Panel>("ButtonPanel")!.Children.OfType<Button>().ElementAt(index)
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
    }

    private static async Task VerifySave(MainWindow window, ShopTracking tracking, string root, byte[] before, bool changed)
    {
        Assert.Equal(changed, tracking.CanUndo);
        var after = ShopFixture.Export(window._shop);
        Assert.True(await window.SaveSessionAsync());
        var loaded = ContinueGshop.Handle(root);
        Assert.Equal(after, ShopFixture.Export(loaded));
        var history = ShopTracking.Load(Path.Combine(root, "tracking.json"));
        if (changed)
        {
            history.Undo(loaded);
            Assert.Equal(before, ShopFixture.Export(loaded));
            history.Redo(loaded);
            Assert.Equal(after, ShopFixture.Export(loaded));
        }
        else Assert.Equal(before, after);
    }

    private static async Task Cleanup(MainWindow window)
    {
        foreach (var dialog in window.OwnedWindows.ToArray()) dialog.Close();
        window.Close();
        await HeadlessWait.UntilAsync(() => !window.IsVisible);
    }

    [Theory]
    [InlineData(false, "Changed", 0, true)]
    [InlineData(false, "Category 0", 0, false)]
    [InlineData(false, " ", 0, false)]
    [InlineData(false, "Changed", 1, false)]
    [InlineData(true, "Changed", 0, true)]
    [InlineData(true, "Subcategory 0", 0, false)]
    [InlineData(true, " ", 0, false)]
    [InlineData(true, "Changed", 1, false)]
    [InlineData(true, "Other", 0, false)]
    public Task RenameFormsPreserveHistoryOnCancellationBlankDuplicateAndNoOp(bool subcategory, string name, int answer, bool changed) => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = ShopFixture.Create(2);
        shop.Categories[0].SubCategories.Add("Other");
        var before = ShopFixture.Export(shop);
        var tracking = new ShopTracking();
        var window = new MainWindow(shop, tracking, new GshopSettings(), directory.Path);
        window.Show();
        try
        {
            Command(window, subcategory ? "Rename SubCategory" : "Rename Category");
            Dialog(window).FindControl<FloatingLabelTextBox>("InputBox")!.Text = name;
            await Answer(window, answer);
            if (subcategory && name == "Other")
            {
                await HeadlessWait.UntilAsync(() => window.OwnedWindows.OfType<CustomMessageBox>().Any());
                await Answer(window);
            }
            await HeadlessWait.UntilAsync(() => !window.OwnedWindows.Any());
            if (changed) Assert.Equal(name, subcategory ? shop.Categories[0].SubCategories[0] : shop.Categories[0].Title);
            await VerifySave(window, tracking, directory.Path, before, changed);
        }
        finally { await Cleanup(window); }
    });

    [Theory]
    [InlineData("Added", 0, false, true)]
    [InlineData("", 0, false, false)]
    [InlineData("Added", 1, false, false)]
    [InlineData("Added", 0, true, false)]
    public Task AddSubcategoryValidatesLimitAndConfirmation(string name, int answer, bool full, bool changed) => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = ShopFixture.Create(1);
        if (full) shop.Categories[0].SubCategories = Enumerable.Range(0, 11).Select(i => $"Sub {i}").ToList();
        var before = ShopFixture.Export(shop);
        var tracking = new ShopTracking();
        var window = new MainWindow(shop, tracking, new GshopSettings(), directory.Path);
        window.Show();
        try
        {
            Command(window, "Add SubCategory");
            if (!full) Dialog(window).FindControl<FloatingLabelTextBox>("InputBox")!.Text = name;
            await Answer(window, answer);
            await HeadlessWait.UntilAsync(() => !window.OwnedWindows.Any());
            Assert.Equal(full ? 11 : changed ? 2 : 1, shop.Categories[0].SubCategories.Count);
            await VerifySave(window, tracking, directory.Path, before, changed);
        }
        finally { await Cleanup(window); }
    });

    [Theory]
    [InlineData(false, 2, true)]
    [InlineData(false, 0, true)]
    [InlineData(false, 2, false)]
    [InlineData(true, 2, true)]
    [InlineData(true, 0, true)]
    [InlineData(true, 2, false)]
    public Task ResetFormsPersistReversibleChangesAndPreserveCancelledShops(bool subcategory, int count, bool accept) => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = ShopFixture.Create(count);
        shop.Itens.ForEach(item => item.CategoryIndex = 0);
        var before = ShopFixture.Export(shop);
        var tracking = new ShopTracking();
        var window = new MainWindow(shop, tracking, new GshopSettings(), directory.Path);
        window.Show();
        try
        {
            Command(window, subcategory ? "Reset SubCategory" : "Reset Category");
            await Answer(window, accept ? 0 : 1);
            if (accept)
            {
                await HeadlessWait.UntilAsync(() => window.OwnedWindows.OfType<CustomMessageBox>().Any());
                await Answer(window);
            }
            await HeadlessWait.UntilAsync(() => !window.OwnedWindows.Any());
            Assert.Equal(accept ? 0 : count, shop.ItemAmount);
            await VerifySave(window, tracking, directory.Path, before, accept && count > 0);
        }
        finally { await Cleanup(window); }
    });

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public Task DeleteSubcategoryRejectsNonemptyScopesAndRequiresConfirmation(bool hasItems, bool accept) => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = ShopFixture.Create(hasItems ? 1 : 0);
        var before = ShopFixture.Export(shop);
        var tracking = new ShopTracking();
        var window = new MainWindow(shop, tracking, new GshopSettings(), directory.Path);
        window.Show();
        try
        {
            Command(window, "Delete SubCategory");
            await Answer(window, !hasItems && !accept ? 1 : 0);
            await HeadlessWait.UntilAsync(() => !window.OwnedWindows.Any());
            Assert.Equal(!hasItems && accept ? 0 : 1, shop.Categories[0].SubCategories.Count);
            await VerifySave(window, tracking, directory.Path, before, !hasItems && accept);
        }
        finally { await Cleanup(window); }
    });
}
