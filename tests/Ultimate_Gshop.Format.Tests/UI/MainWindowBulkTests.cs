using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Itens;
using Ultimate_Gshop.Views;
using Ultimate_Gshop.Views.UserControls;

namespace Ultimate_Gshop.Format.Tests;

[Collection("Image rendering")]
public sealed class MainWindowBulkTests(ImageRenderingFixture fixture)
{
    private static async Task PumpAsync() => await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
    private static Shop Create()
    {
        var shop = ShopFixture.Create(3);
        shop.Itens.ForEach(item => { item.ID = 42; item.CategoryIndex = 0; });
        return shop;
    }
    private static void Click(MainWindow window, string name) => window.FindControl<Button>(name)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void Select(MainWindow window, Item item, bool control = false)
    {
        window.UpdateLayout();
        var card = window.GetVisualDescendants().OfType<Grid>().Single(grid => ReferenceEquals(grid.Tag, item));
        var point = card.TranslatePoint(new Point(card.Bounds.Width / 2, card.Bounds.Height / 2), window)!.Value;
        var modifiers = control ? RawInputModifiers.Control : RawInputModifiers.None;
        window.MouseDown(point, MouseButton.Left, modifiers);
        window.MouseUp(point, MouseButton.Left, modifiers);
    }
    private static void Bulk(MainWindow window, string header)
    {
        var card = window.GetVisualDescendants().OfType<Grid>().First(grid => grid.Tag is Item);
        var roots = card.ContextMenu!.Items.OfType<MenuItem>();
        var command = roots.SelectMany(root => new[] { root }.Concat(root.Items.OfType<MenuItem>())).Single(item => Equals(item.Header, header));
        command.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
    }
    private static void Answer(MainWindow window, int index = 0) => window.OwnedWindows.OfType<CustomMessageBox>().Single()
        .FindControl<Panel>("ButtonPanel")!.Children.OfType<Button>().ElementAt(index).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static async Task Cleanup(MainWindow window)
    {
        foreach (var owned in window.OwnedWindows.ToArray()) owned.Close();
        await PumpAsync();
        if (!window.IsVisible) return;
        Click(window, "DetailUndoButton");
        window.Close();
        await HeadlessWait.UntilAsync(() => !window.IsVisible);
    }

    [Theory]
    [InlineData("Amount", "9", 9)]
    [InlineData("Limit Period", "14", 14)]
    [InlineData("Avail Frequency", "4", 4)]
    [InlineData("Price", "1.25", 125)]
    public Task BulkFormChangesOnlySelectedDuplicateIdsAndSupportsHistory(string command, string text, int expected) => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = Create();
        var before = ShopFixture.Export(shop);
        var hidden = shop.Itens[2].Clone();
        var history = new ShopTracking();
        var window = new MainWindow(shop, history, new GshopSettings(), directory.Path);
        try
        {
            window.Show();
            await PumpAsync();
            Select(window, shop.Itens[0]);
            Select(window, shop.Itens[1], true);
            Assert.Equal(2, window.FindControl<ItemsListControl>("ItemsList")!.SelectedItems.Count);
            Bulk(window, command);
            window.OwnedWindows.OfType<CustomMessageBox>().Single().FindControl<FloatingLabelTextBox>("InputBox")!.Text = text;
            Answer(window);
            await PumpAsync();
            int Value(Item item) => command switch
            {
                "Amount" => item.Amount, "Limit Period" => item.LimitPeriod,
                "Avail Frequency" => item.AvailFrequency, _ => item.Sales[0].Price
            };
            Assert.Equal(expected, Value(shop.Itens[0]));
            Assert.Equal(expected, Value(shop.Itens[1]));
            Assert.True(ItemComparer.Handle(hidden, shop.Itens[2]));
            Click(window, "UndoButton");
            Click(window, "UndoButton");
            Assert.Equal(before, ShopFixture.Export(shop));
            Click(window, "RedoButton");
            Click(window, "RedoButton");
            Assert.Equal(expected, Value(shop.Itens[1]));
        }
        finally { await Cleanup(window); }
    });

    [Fact]
    public Task CloneThenDeletePreservesRecordIdentityAndCounts() => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = Create();
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        var window = new MainWindow(shop, history, new GshopSettings(), directory.Path);
        try
        {
            window.Show();
            await PumpAsync();
            Select(window, shop.Itens[0]);
            Select(window, shop.Itens[1], true);
            Bulk(window, "Clone Multiple");
            Answer(window);
            await PumpAsync();
            Assert.Equal(5, shop.ItemAmount);
            Assert.Equal(new[] { 3, 4 }, shop.Itens.Skip(3).Select(item => item.Place));
            Select(window, shop.Itens[3]);
            Select(window, shop.Itens[4], true);
            Bulk(window, "Delete Multiple");
            Answer(window);
            await PumpAsync();
            Assert.Equal(3, shop.ItemAmount);
            Assert.Equal(before, ShopFixture.Export(shop));
            for (var index = 0; index < 4; index++) Click(window, "UndoButton");
            Assert.Equal(before, ShopFixture.Export(shop));
        }
        finally { await Cleanup(window); }
    });

    [Fact]
    public Task ClosingAdvancedPricesDoesNotAskToApplyOrChangeTheShop() => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = Create();
        var before = ShopFixture.Export(shop);
        var window = new MainWindow(shop, new ShopTracking(), new GshopSettings(), directory.Path);
        try
        {
            window.Show();
            await PumpAsync();
            Select(window, shop.Itens[0]);
            Bulk(window, "Advanced Price");
            window.OwnedWindows.OfType<SaleEditorWindow>().Single().Close();
            await PumpAsync();
            Assert.Empty(window.OwnedWindows);
            Assert.Equal(before, ShopFixture.Export(shop));
        }
        finally { await Cleanup(window); }
    });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task AdvancedPricesApplyIndependentSalesOnlyAfterConfirmationAndPersistHistory(bool accept) => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = Create();
        var before = ShopFixture.Export(shop);
        var tracking = new ShopTracking();
        var window = new MainWindow(shop, tracking, new GshopSettings(), directory.Path);
        try
        {
            window.Show();
            await PumpAsync();
            Select(window, shop.Itens[0]);
            Select(window, shop.Itens[1], true);
            Bulk(window, "Advanced Price");
            var editor = window.OwnedWindows.OfType<SaleEditorWindow>().Single();
            editor.UpdateLayout();
            editor.FindControl<StyledComboBox>("TypeValueComboBox")!.SelectedItem = "Price";
            editor.FindControl<FloatingLabelTextBox>("ValueTextBox")!.Text = "2.50";
            editor.GetVisualDescendants().OfType<Button>().Single(button => button.Width == 35 && button.Height == 35)
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(before, ShopFixture.Export(shop));
            editor.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "CloseButton")
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await HeadlessWait.UntilAsync(() => window.OwnedWindows.OfType<CustomMessageBox>().Any());
            Answer(window, accept ? 0 : 1);
            await PumpAsync();
            Assert.Equal(accept, tracking.CanUndo);
            if (accept)
            {
                Assert.Equal(250, shop.Itens[0].Sales[0].Price);
                Assert.Equal(250, shop.Itens[1].Sales[0].Price);
                Assert.NotSame(shop.Itens[0].Sales[0], shop.Itens[1].Sales[0]);
                Assert.Equal(1000, shop.Itens[2].Sales[0].Price);
            }
            var after = ShopFixture.Export(shop);
            Assert.True(await window.SaveSessionAsync());
            var loaded = Ultimate_Gshop.Gshop.Services.ContinueGshop.Handle(directory.Path);
            var history = ShopTracking.Load(Path.Combine(directory.Path, "tracking.json"));
            Assert.Equal(after, ShopFixture.Export(loaded));
            if (accept)
            {
                history.Undo(loaded);
                history.Undo(loaded);
                Assert.Equal(before, ShopFixture.Export(loaded));
                history.Redo(loaded);
                history.Redo(loaded);
                Assert.Equal(after, ShopFixture.Export(loaded));
            }
            else Assert.Equal(before, after);
        }
        finally { await Cleanup(window); }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task BulkEditAsksBeforeDiscardingAnUnsavedDraft(bool accept) => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = Create();
        var before = ShopFixture.Export(shop);
        var window = new MainWindow(shop, new ShopTracking(), new GshopSettings(), directory.Path);
        try
        {
            window.Show();
            await PumpAsync();
            Select(window, shop.Itens[0]);
            Click(window, "DetailEditButton");
            var name = window.FindControl<FloatingLabelTextBox>("DetailName")!;
            name.Text = "Unsaved draft";
            Bulk(window, "Amount");
            var dialog = window.OwnedWindows.OfType<CustomMessageBox>().Single();
            Assert.Equal("Descartar Alterações?", dialog.FindControl<TextBlock>("TitleBlock")!.Text);
            Answer(window, accept ? 0 : 1);
            await PumpAsync();
            if (accept)
            {
                window.OwnedWindows.OfType<CustomMessageBox>().Single().FindControl<FloatingLabelTextBox>("InputBox")!.Text = "9";
                Answer(window);
                await PumpAsync();
                Assert.Equal(9, shop.Itens[0].Amount);
                Assert.Equal("中文 Item 0", shop.Itens[0].Name);
            }
            else
            {
                Assert.Equal("Unsaved draft", name.Text);
                Assert.Equal(before, ShopFixture.Export(shop));
            }
            Assert.Empty(window.OwnedWindows);
        }
        finally { await Cleanup(window); }
    });

    [Fact]
    public Task MovingSelectedItemsUpdatesBothCategoryIndicesAndCanBeUndone() => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = Create();
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        var window = new MainWindow(shop, history, new GshopSettings(), directory.Path);
        try
        {
            window.Show();
            await PumpAsync();
            Select(window, shop.Itens[0]);
            Select(window, shop.Itens[1], true);
            Bulk(window, "Move Multiple");
            var dialog = window.OwnedWindows.OfType<CustomMessageBox>().Single();
            dialog.FindControl<ComboBox>("ComboBox1")!.SelectedItem = shop.Categories[1].Title;
            dialog.FindControl<ComboBox>("ComboBox2")!.SelectedItem = shop.Categories[1].SubCategories[0];
            Answer(window);
            await PumpAsync();
            Assert.Equal(1, shop.Itens[0].CategoryIndex);
            Assert.Equal(1, shop.Itens[1].CategoryIndex);
            Assert.Equal(0, shop.Itens[0].SubCategoryIndex);
            Assert.Equal(0, shop.Itens[2].CategoryIndex);
            Assert.Same(shop.Itens[2], Assert.Single(window.FindControl<ItemsListControl>("ItemsList")!.ItemsSource!));
            Click(window, "UndoButton");
            Click(window, "UndoButton");
            Assert.Equal(before, ShopFixture.Export(shop));
        }
        finally { await Cleanup(window); }
    });
}
