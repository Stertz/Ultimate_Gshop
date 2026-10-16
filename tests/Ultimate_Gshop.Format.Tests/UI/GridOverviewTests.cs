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
            Assert.Equal(before, ShopFixture.Export(shop));
            var row = model.Items[0];
            Assert.Equal(new GridItemSnapshot(0, 1000, "中文 Item 0", 2, 0, 0), row);
            Assert.Throws<NotSupportedException>(() => ((IList<GridItemSnapshot>)model.Items).Clear());
            shop.Itens[0].Name = "Later edit";
            shop.Itens[0].Amount = 99;
            shop.Itens[0].Sales[0].Price = 1;
            shop.Itens.Clear();
            Assert.Equal("中文 Item 0", row.Name);
            Assert.Equal(2, row.Amount);
            Assert.Equal(2, model.ItemCount);
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
