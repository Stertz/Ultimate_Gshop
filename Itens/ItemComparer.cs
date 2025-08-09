using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultimate_Gshop.Itens
{
    public static class ItemComparer
    {
        public static bool Handle(Item? x, Item? y)
        {
            if (x is null || y is null) return false;
            return x.Amount == y.Amount &&
                    x.Place == y.Place &&
                    x.CategoryIndex == y.CategoryIndex &&
                    x.SubCategoryIndex == y.SubCategoryIndex &&
                    x.Icon == y.Icon &&
                    x.Explanation == y.Explanation &&
                    x.Name == y.Name &&
                    x.GiftID == y.GiftID &&
                    x.GiftAmount == y.GiftAmount &&
                    x.GiftTime == y.GiftTime &&
                    x.ILogPrice == y.ILogPrice &&
                    x.LimitPeriod == y.LimitPeriod &&
                    x.AvailFrequency == y.AvailFrequency &&
                    x.Class == y.Class &&
                    x.Reserve1 == y.Reserve1 &&
                    x.Reserve2 == y.Reserve2 &&
                    x.Reserve3 == y.Reserve3 &&
                    x.Reserve4 == y.Reserve4 &&
                    x.Reserve5 == y.Reserve5 &&
                    x.Reserve6 == y.Reserve6 &&
                    x.Sales.SequenceEqual(y.Sales) &&
                    x.OwnerNpcs.SequenceEqual(y.OwnerNpcs);
        }
    }
}
