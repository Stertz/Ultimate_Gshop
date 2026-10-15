namespace Ultimate_Gshop.Itens
{
    public static class ItemComparer
    {
        public static bool Handle(Item? x, Item? y)
        {
            if (x is null || y is null) return false;
            return x.ID == y.ID &&
                    x.Amount == y.Amount &&
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
                    SalesEqual(x.Sales, y.Sales) &&
                    x.OwnerNpcs.SequenceEqual(y.OwnerNpcs);
        }

        private static bool SalesEqual(IReadOnlyList<Sale> first, IReadOnlyList<Sale> second)
        {
            if (first.Count != second.Count) return false;
            for (var index = 0; index < first.Count; index++)
            {
                var x = first[index];
                var y = second[index];
                if (x.Price != y.Price || x.SellingEndTime != y.SellingEndTime ||
                    x.During != y.During || x.SellingStartTime != y.SellingStartTime ||
                    x.Control != y.Control || x.Day != y.Day || x.Status != y.Status ||
                    x.Flags != y.Flags || x.VipLevelRequired != y.VipLevelRequired)
                    return false;
            }
            return true;
        }
    }
}
