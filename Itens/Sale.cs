namespace Ultimate_Gshop.Itens
{
    public class Sale
    {
        public int Price { get; set; }
        public int SellingEndTime { get; set; }
        public int During { get; set; }
        public int SellingStartTime { get; set; }
        public int Control { get; set; }
        public int Day { get; set; }
        public int Status { get; set; }
        public int Flags { get; set; }
        public int VipLevelRequired { get; set; }

        public Sale Clone()
        {
            return new()
            {
                Price = Price,
                SellingEndTime = SellingEndTime,
                During = During,
                SellingStartTime = SellingStartTime,
                Control = Control,
                Day = Day,
                Status = Status,
                Flags = Flags,
                VipLevelRequired = VipLevelRequired
            };
        }

    }
}
