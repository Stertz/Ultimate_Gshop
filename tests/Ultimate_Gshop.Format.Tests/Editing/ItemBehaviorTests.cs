using System.Text.Json;
using Ultimate_Gshop.Itens;

namespace Ultimate_Gshop.Format.Tests;

public sealed class ItemBehaviorTests
{
    [Fact]
    public void ClonePreservesEverySerializedFieldAndOwnsIndependentSalesAndNpcArrays()
    {
        var original = ShopFixture.CreateItem();
        var clone = original.Clone();

        Assert.NotSame(original, clone);
        Assert.Equal(JsonSerializer.Serialize(original), JsonSerializer.Serialize(clone));
        Assert.NotSame(original.Sales, clone.Sales);
        Assert.NotSame(original.Sales[0], clone.Sales[0]);
        Assert.NotSame(original.OwnerNpcs, clone.OwnerNpcs);
        clone.Sales[0].Price = 999;
        clone.OwnerNpcs[0] = 999;
        Assert.Equal(1000, original.Sales[0].Price);
        Assert.Equal(100, original.OwnerNpcs[0]);
    }

    [Fact]
    public void SaleClonePreservesEverySerializedFieldWithoutAliasing()
    {
        var sale = ShopFixture.CreateItem().Sales[0];
        var clone = sale.Clone();
        Assert.NotSame(sale, clone);
        Assert.Equal(JsonSerializer.Serialize(sale), JsonSerializer.Serialize(clone));
    }

    [Theory]
    [InlineData(1000, 1000, true)]
    [InlineData(1000, 1001, false)]
    public void ExistingItemEqualityUsesGameId(int firstId, int secondId, bool expected)
    {
        var first = new Item { ID = firstId };
        var second = new Item { ID = secondId };
        Assert.Equal(expected, first.Equals(second));
        Assert.Equal(expected, first.Equals((object)second));
        if (expected)
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.False(first.Equals(null));
    }

    [Fact]
    public void EmptyAndExpiredSalesHaveNoValidPrice()
    {
        Assert.Equal(0m, new Item().GetValidPrice());
        var item = new Item { Sales = [new Sale { Price = 1234, SellingEndTime = 1 }] };
        Assert.Equal(0m, item.GetValidPrice());
    }

    [Fact]
    public void ActiveSaleHasPriorityAndPricesAreConvertedFromCents()
    {
        var item = new Item { Sales = [new Sale { Price = 100, Status = 0 }, new Sale { Price = 1234, Status = 1 }] };
        Assert.Equal(12.34m, item.GetValidPrice());
    }

    [Fact]
    public void FirstAvailableSaleIsUsedWhenNoneIsActive()
    {
        var item = new Item { Sales = [new Sale { Price = 2345 }, new Sale { Price = 9999 }] };
        Assert.Equal(23.45m, item.GetValidPrice());
    }

    [Fact]
    public void UnmatchedDayIsIgnoredWithoutDependingOnTodaysWeekday()
    {
        var item = new Item { Sales = [new Sale { Price = 9999, Day = int.MaxValue }, new Sale { Price = 250 }] };
        Assert.Equal(2.50m, item.GetValidPrice());
    }
}
