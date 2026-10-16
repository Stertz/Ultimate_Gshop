using Avalonia.Controls;
using Avalonia.VisualTree;
using Ultimate_Gshop.ViewModels;

namespace Ultimate_Gshop.Format.Tests;

[Collection("Image rendering")]
public sealed class GridOverviewTests(ImageRenderingFixture fixture)
{
    [Fact]
    public Task GridIsReadOnlyAndShowsIndependentSnapshotsOfTheShop() => fixture.Run(() =>
    {
        var shop = ShopFixture.Create();
        var before = ShopFixture.Export(shop);
        var window = new GridWindow(shop.Itens);
        try
        {
            window.Show();
            window.UpdateLayout();
            var model = Assert.IsType<GridViewModel>(window.DataContext);
            var grid = window.GetVisualDescendants().OfType<DataGrid>().Single();
            Assert.True(grid.IsReadOnly);
            Assert.False(grid.AutoGenerateColumns);
            Assert.Equal(6, grid.Columns.Count);
            Assert.Same(model.Items, grid.ItemsSource);
            Assert.Equal(2, model.ItemCount);
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == "Read-only snapshot: 2 items");
            Assert.NotSame(shop.Itens[0].Sales[0], model.Items[0].Sales[0]);
            model.Items[0].Name = "Snapshot edit";
            model.Items[0].Sales[0].Price = 1;
            Assert.Equal(before, ShopFixture.Export(shop));
        }
        finally { window.Close(); }
    });

    [Fact]
    public Task DefaultGridHasAnEmptySnapshot() => fixture.Run(() =>
    {
        var window = new GridWindow();
        try
        {
            window.Show();
            Assert.Empty(Assert.IsType<GridViewModel>(window.DataContext).Items);
        }
        finally { window.Close(); }
    });
}
