using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Globalization;
using Ultimate_Gshop.Itens;
using Ultimate_Gshop.Views.UserControls;

namespace Ultimate_Gshop.Format.Tests;

[Collection("Image rendering")]
public sealed class SaleEditorTests(ImageRenderingFixture fixture)
{
    private static void ClickAdd(SaleEditorWindow window) =>
        window.GetVisualDescendants().OfType<Button>().Single(button => button.Width == 35 && button.Height == 35)
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    [Fact]
    public Task SaleRowsDisplayCentsAsCurrencyAndSplitDurations() => fixture.Run(() =>
    {
        var item = ShopFixture.CreateItem();
        item.Sales[0].Price = 1234;
        item.Sales[0].During = 7200;
        item.Sales[1].During = 0;
        item.Sales[2].During = 61;
        item.Sales[3].During = 604800;
        var window = new SaleEditorWindow(item);
        try
        {
            window.Show();
            window.UpdateLayout();
            Assert.Equal("12.34", window.FindControl<TextBlock>("SaleValidPrice")!.Text);
            var rows = window.FindControl<ItemsControl>("SalesList")!;
            Assert.Equal(4, rows.Items.Count);
            var text = rows.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text).ToList();
            Assert.Contains(12.34m.ToString(CultureInfo.CurrentCulture), text);
            Assert.Contains("Hour", text);
            Assert.Contains("Minute", text);
            Assert.Contains("Week", text);
            Assert.Contains("2", text);
        }
        finally { window.Close(); }
    });

    [Fact]
    public Task SaleCreationUsesTheVisibleFormValuesAndPreservesExistingSales() => fixture.Run(() =>
    {
        var item = ShopFixture.CreateItem();
        var existing = item.Sales[0];
        var window = new SaleEditorWindow(item);
        try
        {
            window.Show();
            window.UpdateLayout();
            window.FindControl<FloatingLabelTextBox>("PriceTextBox")!.Text = "12,34";
            window.FindControl<FloatingLabelTextBox>("DayTextBox")!.Text = "3";
            window.FindControl<FloatingLabelTextBox>("TimeTextBox")!.Text = "2";
            window.FindControl<FloatingLabelTextBox>("FlagsTextBox")!.Text = "7";
            window.FindControl<FloatingLabelTextBox>("VipLevelTextBox")!.Text = "2";
            window.FindControl<StyledComboBox>("SetTimeComboBox")!.SelectedItem = "Hour";
            window.FindControl<StyledComboBox>("StatusComboBox")!.SelectedIndex = 2;
            window.FindControl<StyledComboBox>("ControlComboBox")!.SelectedIndex = 1;
            window.FindControl<StyledDatePicker>("SalesStartPicker")!.SelectedDate = new DateTime(2024, 2, 29);
            window.FindControl<StyledDatePicker>("SalesEndPicker")!.SelectedDate = new DateTime(2025, 12, 31);
            ClickAdd(window);
            var added = item.Sales[^1];
            Assert.Equal(5, item.Sales.Count);
            Assert.Same(existing, item.Sales[0]);
            Assert.Equal(1234, added.Price);
            Assert.Equal(7200, added.During);
            Assert.Equal(3, added.Day);
            Assert.Equal(7, added.Flags);
            Assert.Equal(2, added.VipLevelRequired);
            Assert.Equal(2, added.Status);
            Assert.Equal(1, added.Control);
            // Characterize the current picker contract, without certifying game time semantics.
            Assert.Equal(20240229, added.SellingStartTime);
            Assert.Equal(20251231, added.SellingEndTime);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData("Price", "12.34", 1234)]
    [InlineData("Add %", "10", 1100)]
    [InlineData("Remove %", "10", 900)]
    [InlineData("Multiply", "2.5", 2500)]
    [InlineData("Divide", "4", 250)]
    [InlineData("Divide", "0", 1000)]
    public Task AdvancedPriceFormUpdatesOnlyTheFirstSale(string operation, string value, int expected) => fixture.Run(() =>
    {
        var item = ShopFixture.CreateItem();
        var second = item.Sales[1].Clone();
        var window = new SaleEditorWindow(item);
        try
        {
            window.Show();
            window.UpdateLayout();
            window.FindControl<StyledComboBox>("TypeValueComboBox")!.SelectedItem = operation;
            window.FindControl<FloatingLabelTextBox>("ValueTextBox")!.Text = value;
            ClickAdd(window);
            Assert.Equal(4, item.Sales.Count);
            Assert.Equal(expected, item.Sales[0].Price);
            Assert.Equal(second.Price, item.Sales[1].Price);
            Assert.Equal(second.During, item.Sales[1].During);
            Assert.Null(window.FindControl<StyledComboBox>("TypeValueComboBox")!.SelectedItem);
        }
        finally { window.Close(); }
    });

    [Fact]
    public Task EmptySaleListSupportsTheAdvancedPriceFormAndNullItemIsANoOp() => fixture.Run(() =>
    {
        var item = new Item();
        var window = new SaleEditorWindow(item);
        try
        {
            window.Show();
            window.UpdateLayout();
            Assert.Equal("0.00", window.FindControl<TextBlock>("SaleValidPrice")!.Text);
            window.FindControl<StyledComboBox>("TypeValueComboBox")!.SelectedItem = "Price";
            window.FindControl<FloatingLabelTextBox>("ValueTextBox")!.Text = "1.25";
            ClickAdd(window);
            Assert.Equal(125, Assert.Single(item.Sales).Price);
        }
        finally { window.Close(); }
        var empty = new SaleEditorWindow();
        try
        {
            empty.Show();
            empty.UpdateLayout();
            ClickAdd(empty);
            Assert.Empty(empty.FindControl<ItemsControl>("SalesList")!.Items);
        }
        finally { empty.Close(); }
    });
}
