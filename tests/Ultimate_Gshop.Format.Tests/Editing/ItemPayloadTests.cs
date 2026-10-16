using Ultimate_Gshop.Itens;
using Ultimate_Gshop.Itens.Services;

namespace Ultimate_Gshop.Format.Tests;

public sealed class ItemPayloadTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(6)]
    public void SalesKeepTheirOrderWithinFourSlotsAndTakeHighestPricesWhenOversized(int count)
    {
        var source = Enumerable.Range(0, count).Select(index => new Sale { Price = index + 1 }).ToList();
        var normalized = ItemPayload.NormalizeSales(source);
        var expected = (count > 4 ? source.OrderByDescending(sale => sale.Price).Take(4) : source)
            .Select(sale => sale.Price).Concat(Enumerable.Repeat(0, Math.Max(0, 4 - count)));
        Assert.Equal(expected, normalized.Select(sale => sale.Price));
        normalized[0].Price = 999;
        Assert.Equal(Enumerable.Range(1, count), source.Select(sale => sale.Price));
    }

    [Fact]
    public void EmptyDraftCreatesFourIndependentSlots()
    {
        var sales = ItemPayload.NormalizeSales(null);
        Assert.Equal(4, sales.Count);
        sales[0].Price = 1;
        Assert.All(sales.Skip(1), sale => Assert.Equal(0, sale.Price));
    }
}
