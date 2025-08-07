namespace Ultimate_Gshop.Itens
{
    public class Item
    {
        public int ID { get; set; }
        public int Amount { get; set; }
        public int Place { get; set; }
        public int CategoryIndex { get; set; }
        public int SubCategoryIndex { get; set; }
        public string Icon { get; set; } = string.Empty;
        public List<Sale> Sales { get; set; } = [];
        public string Explanation { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int GiftID { get; set; }
        public int GiftAmount { get; set; }
        public int GiftTime { get; set; }
        public int ILogPrice { get; set; }
        public int[] OwnerNpcs { get; set; } = [];
        public int LimitPeriod { get; set; }
        public int AvailFrequency { get; set; }
        public int Class { get; set; }
        public int Reserve1 { get; set; } 
        public int Reserve2 { get; set; } 
        public int Reserve3 { get; set; } 
        public int Reserve4 { get; set; }
        public int Reserve5 { get; set; }
        public int Reserve6 { get; set; }

        public Item Clone()
        {
            return new()
            {
                Place = Place,
                CategoryIndex = CategoryIndex,
                SubCategoryIndex = SubCategoryIndex,
                Icon = Icon,
                ID = ID,
                Amount = Amount,
                //Sales = [Sales[0].Clone(), Sales[1].Clone(), Sales[2].Clone(), Sales[3].Clone()],
                Explanation = Explanation,
                Name = Name,
                GiftID = GiftID,
                GiftAmount = GiftAmount,
                GiftTime = GiftTime,
                ILogPrice = ILogPrice,
                OwnerNpcs = OwnerNpcs,
                LimitPeriod = LimitPeriod,
                AvailFrequency = AvailFrequency,
                Class = Class,
                Reserve1 = Reserve1,
                Reserve2 = Reserve2,
                Reserve3 = Reserve3,
                Reserve4 = Reserve4,
                Reserve5 = Reserve5,
                Reserve6 = Reserve6
            };
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Item);
        }

        public bool Equals(Item? other)
        {
            if (other is null)
                return false;

            return this.ID == other.ID;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

    }
}
