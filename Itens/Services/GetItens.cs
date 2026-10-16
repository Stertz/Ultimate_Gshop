using System.Text;
using Ultimate_Gshop.Misc;
namespace Ultimate_Gshop.Itens.Services
{
    using Ultimate_Gshop.Gshop.Misc;
    public static class GetItens
    {
        public static IEnumerable<Item> Handle(int itemAmount, BinaryReader br, FileStream fs)
        {

            var itens = new List<Item>();
            Span<byte> textBytes = stackalloc byte[1024];
            Span<char> textChars = stackalloc char[1025];
            var iconEncoding = Encoding.GetEncoding(936);

            BinaryFormat.GetCategoryOffset(fs, itemAmount);
            fs.Seek(BinaryFormat.HeaderSize, SeekOrigin.Begin);

            for (int i = 0; i < itemAmount; i++)
            {

                var item = new Item
                {
                    Place = br.ReadInt32(),

                    CategoryIndex = br.ReadInt32(),
                    SubCategoryIndex = br.ReadInt32(),
                    Icon = BinaryFormat.ReadTextField(br, textBytes[..128], textChars, iconEncoding),
                    ID = br.ReadInt32(),
                    Amount = br.ReadInt32(),
                    Sales = new List<Sale>(Variables.MaxSalesPerItem)
                };

                for (int g = 0; g < item.Sales.Capacity; g++)
                {
                    var sale = new Sale
                    {
                        Price = br.ReadInt32(),
                        SellingEndTime = br.ReadInt32(),
                        During = br.ReadInt32(),
                        SellingStartTime = br.ReadInt32(),
                        Control = br.ReadInt32(),
                        Day = br.ReadInt32(),
                        Status = br.ReadInt32()
                    };
                    if (sale.Status > 19) sale.Status = 0;
                    sale.Flags = br.ReadInt32();
                    sale.VipLevelRequired = br.ReadInt32();
                    sale.Control = g == 0 ? -1 : 0;
                    item.Sales.Add(sale);
                }

                item.Explanation = BinaryFormat.ReadTextField(br, textBytes, textChars, Encoding.Unicode, trimEndOnly: true);
                item.Name = BinaryFormat.ReadTextField(br, textBytes[..64], textChars, Encoding.Unicode);

                item.GiftID = br.ReadInt32();
                item.GiftAmount = br.ReadInt32();
                item.GiftTime = br.ReadInt32();

                item.ILogPrice = br.ReadInt32();

                item.OwnerNpcs = new int[8];

                for (int g = 0; g < 8; g++)
                {
                    item.OwnerNpcs[g] = br.ReadInt32();
                }

                item.LimitPeriod = br.ReadInt32();
                item.AvailFrequency = br.ReadInt32();

                item.Reserve1 = br.ReadInt32();
                item.Reserve2 = br.ReadInt32();

                item.Class = br.ReadInt32();

                item.Reserve3 = br.ReadInt32();
                item.Reserve4 = br.ReadInt32();
                item.Reserve5 = br.ReadInt32();
                item.Reserve6 = br.ReadInt32();

                item.Place = i;

                itens.Add(item);

            }

            return itens;

        }
    }
}
