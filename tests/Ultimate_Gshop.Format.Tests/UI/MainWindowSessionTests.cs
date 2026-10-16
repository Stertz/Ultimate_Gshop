using Avalonia.Controls;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Itens.Services;
using Ultimate_Gshop.Views;
using Ultimate_Gshop.Views.UserControls;
using Avalonia.VisualTree;

namespace Ultimate_Gshop.Format.Tests;

[Collection("Image rendering")]
public sealed class MainWindowSessionTests(ImageRenderingFixture fixture)
{
    private static void Click(MainWindow window, string name) =>
        window.FindControl<Button>(name)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static void Answer(MainWindow window, int button = 0) =>
        window.OwnedWindows.OfType<CustomMessageBox>().Single().FindControl<Panel>("ButtonPanel")!
            .Children.OfType<Button>().ElementAt(button).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static async Task PumpAsync() => await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

    private static void SelectCard(MainWindow window, Itens.Item item)
    {
        window.UpdateLayout();
        var card = window.GetVisualDescendants().OfType<Grid>().Single(grid => ReferenceEquals(grid.Tag, item));
        var point = card.TranslatePoint(new Point(card.Bounds.Width / 2, card.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
    }

    private static async Task CloseAsync(MainWindow window)
    {
        if (!window.IsVisible) return;
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        window.Closed += (_, _) => closed.TrySetResult();
        window.Close();
        if (window.OwnedWindows.OfType<CustomMessageBox>().Any()) Answer(window);
        await closed.Task.WaitAsync(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public Task UnopenedEditorCannotSaveOrCreateSessionFilesWhenClosed() => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var window = new MainWindow(ShopFixture.Create(), new ShopTracking(), new GshopSettings(), directory.Path);
        Assert.False(await window.SaveSessionAsync());
        window.Close();
        Assert.Empty(Directory.EnumerateFileSystemEntries(directory.Path));
    });

    [Fact]
    public Task InjectedSessionSearchAndHistoryWorkThroughTheWindowControls() => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = ShopFixture.Create(3);
        shop.Itens.ForEach(item => item.CategoryIndex = 0);
        shop.Itens[0].Name = "Red dragon";
        shop.Itens[1].Name = "Blue dragon";
        shop.Itens[2].Name = "Wolf";
        var history = new ShopTracking();
        var window = new MainWindow(shop, history, new GshopSettings(), directory.Path);
        try
        {
            window.Show();
            await PumpAsync();
            var list = window.FindControl<ItemsListControl>("ItemsList")!;
            var search = window.FindControl<FloatingLabelTextBox>("SearchBox")!;
            void Search(string text)
            {
                search.Text = text;
                search.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Back });
            }
            Assert.Equal(3, list.ItemsSource!.Count());
            Search("Red dragon");
            Assert.Same(shop.Itens[0], Assert.Single(list.ItemsSource!));
            Search("dragon");
            Assert.Equal(2, list.ItemsSource!.Count());
            Search("wolf");
            Assert.Same(shop.Itens[2], Assert.Single(list.ItemsSource!));
            var changed = shop.Itens[2].Clone();
            changed.Name = "Changed";
            UpdateItem.Handle(shop.Itens, changed, shop.Itens[2], history);
            Assert.True(window.FindControl<Button>("UndoButton")!.IsEnabled);
            Click(window, "UndoButton");
            Assert.Equal("Wolf", Assert.Single(list.ItemsSource!).Name);
            Click(window, "RedoButton");
            Assert.Empty(list.ItemsSource!);
            Search("");
            Assert.Equal(3, list.ItemsSource!.Count());
            Assert.True(await window.SaveSessionAsync());
            Assert.Equal(3, ContinueGshop.Handle(directory.Path).Itens.Count);
        }
        finally { await CloseAsync(window); }
    });

    [Fact]
    public Task CreatingAnItemAndUndoRedoUseTheActualEditorForm() => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = ShopFixture.Create(1);
        var history = new ShopTracking();
        var window = new MainWindow(shop, history, new GshopSettings(), directory.Path);
        try
        {
            window.Show();
            await PumpAsync();
            Click(window, "DetailNewButton");
            window.FindControl<FloatingLabelTextBox>("DetailID")!.Text = "2000";
            window.FindControl<FloatingLabelTextBox>("DetailName")!.Text = "Created";
            window.FindControl<FloatingLabelTextBox>("DetailAmount")!.Text = "5";
            Click(window, "DetailSaveButton");
            Answer(window);
            await PumpAsync();
            Assert.Equal(2, shop.ItemAmount);
            Assert.Equal("Created", shop.Itens[1].Name);
            Assert.Equal(2000, shop.Itens[1].ID);
            Assert.Equal(4, shop.Itens[1].Sales.Count);
            Assert.Equal(8, shop.Itens[1].OwnerNpcs.Length);
            Click(window, "UndoButton");
            Assert.Single(shop.Itens);
            Click(window, "RedoButton");
            Assert.Equal(2, shop.Itens.Count);
            Assert.True(await window.SaveSessionAsync());
        }
        finally { await CloseAsync(window); }
    });

    [Fact]
    public Task SaveFailureKeepsTheWindowOpenAndRejectsConcurrentAttempts() => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = ShopFixture.Create(1);
        var window = new MainWindow(shop, new ShopTracking(), new GshopSettings { AutoSave = true, SaveFromSeconds = -1 }, directory.Path);
        var blocker = Path.Combine(directory.Path, "gshop_temp.data");
        try
        {
            window.Show();
            await PumpAsync();
            Directory.CreateDirectory(blocker);
            var pending = window.SaveSessionAsync();
            Assert.False(pending.IsCompleted);
            await HeadlessWait.UntilAsync(() => window.OwnedWindows.OfType<CustomMessageBox>().Any());
            Assert.Single(window.OwnedWindows.OfType<CustomMessageBox>());
            Assert.False(await window.SaveSessionAsync());
            window.Close();
            Assert.True(window.IsVisible);
            Assert.Single(window.OwnedWindows.OfType<CustomMessageBox>());
            Answer(window);
            Assert.False(await pending);
            Assert.True(window.IsVisible);
            Directory.Delete(blocker);
            Assert.True(await window.SaveSessionAsync());
            Assert.Single(ContinueGshop.Handle(directory.Path).Itens);
        }
        finally
        {
            if (Directory.Exists(blocker)) Directory.Delete(blocker);
            foreach (var dialog in window.OwnedWindows.ToArray()) dialog.Close();
            await CloseAsync(window);
        }
        var undoButton = window.FindControl<Button>("UndoButton")!;
        Assert.False(await window.SaveSessionAsync());
        Assert.False(undoButton.IsEnabled);
    });

    [Fact]
    public Task FailedClosePreservesTheSessionAndCanBeRetried() => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = ShopFixture.Create(1);
        var history = new ShopTracking();
        var window = new MainWindow(shop, history, new GshopSettings(), directory.Path);
        var blocker = Path.Combine(directory.Path, "gshop_temp.data");
        try
        {
            window.Show();
            await PumpAsync();
            Directory.CreateDirectory(blocker);
            window.Close();
            Assert.True(window.IsVisible);
            await HeadlessWait.UntilAsync(() => window.OwnedWindows.OfType<CustomMessageBox>().Any());
            Assert.Equal("Could not save the shop", window.OwnedWindows.OfType<CustomMessageBox>().Single().FindControl<TextBlock>("TitleBlock")!.Text);
            // Repeated close requests must not open duplicate confirmation/error dialogs.
            window.Close();
            Assert.Single(window.OwnedWindows.OfType<CustomMessageBox>());
            Answer(window);
            await PumpAsync();
            Assert.True(window.IsVisible);
            Directory.Delete(blocker);
            await CloseAsync(window);
            Assert.True(File.Exists(Path.Combine(directory.Path, "tracking.json")));
            Assert.True(File.Exists(Path.Combine(directory.Path, "settings.json")));
            Assert.Equal(ShopFixture.Export(shop), ShopFixture.Export(ContinueGshop.Handle(directory.Path)));
            var button = window.FindControl<Button>("UndoButton")!;
            history.ItemCreated(1, ShopFixture.CreateItem(1));
            Assert.False(button.IsEnabled); // A closed window must no longer observe external history.
        }
        finally
        {
            if (Directory.Exists(blocker)) Directory.Delete(blocker);
            foreach (var dialog in window.OwnedWindows.ToArray()) dialog.Close();
            await CloseAsync(window);
        }
    });

    [Fact]
    public Task CancellingDiscardKeepsTheEditorOpenAndAnEmptySubcategoryListIsValid() => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = ShopFixture.Create(0);
        shop.Categories[0].SubCategories.Clear();
        var window = new MainWindow(shop, new ShopTracking(), new GshopSettings(), directory.Path);
        try
        {
            window.Show();
            await PumpAsync();
            Assert.Equal(shop.Categories.Count, window.FindControl<ItemsControl>("CategoryList")!.Items.Count);
            Assert.Equal(-1, window.FindControl<StyledComboBox>("DetailSubCategoryComboBox")!.SelectedIndex);
            Click(window, "DetailNewButton");
            window.Close();
            Answer(window, 1);
            await PumpAsync();
            Assert.True(window.IsVisible);
            Assert.False(File.Exists(Path.Combine(directory.Path, "gshop_temp.data")));
            Click(window, "DetailUndoButton");
        }
        finally { await CloseAsync(window); }
    });

    [Fact]
    public Task PointerSelectionEditDiscardAndDeleteKeepDuplicateIdsDistinct() => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = ShopFixture.Create(2);
        shop.Itens[1].CategoryIndex = 0;
        shop.Itens[1].ID = shop.Itens[0].ID;
        var before = ShopFixture.Export(shop);
        var history = new ShopTracking();
        var window = new MainWindow(shop, history, new GshopSettings(), directory.Path);
        try
        {
            window.Show();
            await PumpAsync();
            SelectCard(window, shop.Itens[1]);
            var name = window.FindControl<FloatingLabelTextBox>("DetailName")!;
            Assert.Equal(shop.Itens[1].Name, name.Text);
            Click(window, "DetailEditButton");
            name.Text = "Discarded draft";
            Click(window, "DetailUndoButton");
            Assert.Equal(before, ShopFixture.Export(shop));
            Click(window, "DetailEditButton");
            name.Text = "Second record edited";
            Click(window, "DetailSaveButton");
            Answer(window);
            await PumpAsync();
            Assert.Equal("中文 Item 0", shop.Itens[0].Name);
            Assert.Equal("Second record edited", shop.Itens[1].Name);
            Click(window, "UndoButton");
            Assert.Equal(before, ShopFixture.Export(shop));
            SelectCard(window, shop.Itens[1]);
            Click(window, "DetailDeleteButton");
            Answer(window);
            await PumpAsync();
            Assert.Equal(1, shop.ItemAmount);
            Assert.Equal("中文 Item 0", Assert.Single(shop.Itens).Name);
            Click(window, "UndoButton");
            Assert.Equal(before, ShopFixture.Export(shop));
        }
        finally { await CloseAsync(window); }
    });

    [Fact]
    public Task SerializationFailurePreservesThePreviouslySavedSession() => fixture.Run(async () =>
    {
        using var directory = new TestDirectory();
        var shop = ShopFixture.Create(1);
        var window = new MainWindow(shop, new ShopTracking(), new GshopSettings(), directory.Path);
        var originalSales = shop.Itens[0].Sales.Select(sale => sale.Clone()).ToList();
        try
        {
            window.Show();
            await PumpAsync();
            Assert.True(await window.SaveSessionAsync());
            var path = Path.Combine(directory.Path, "gshop_temp.data");
            var previous = File.ReadAllBytes(path);
            shop.Itens[0].Sales.Clear();
            var pending = window.SaveSessionAsync();
            await HeadlessWait.UntilAsync(() => window.OwnedWindows.OfType<CustomMessageBox>().Any());
            Assert.Single(window.OwnedWindows.OfType<CustomMessageBox>());
            Answer(window);
            Assert.False(await pending);
            Assert.Equal(previous, File.ReadAllBytes(path));
            Assert.True(window.IsVisible);
        }
        finally
        {
            shop.Itens[0].Sales = originalSales;
            foreach (var dialog in window.OwnedWindows.ToArray()) dialog.Close();
            await CloseAsync(window);
        }
    });
}
