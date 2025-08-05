using System.IO;
using System.Text;
using Ultimate_Gshop.Misc;
namespace Ultimate_Gshop.Itens.Services
{
    public static class GetItens
    {
        public static IEnumerable<Item> Handle(int itemAmount, BinaryReader br, FileStream fs)
        {

            var itens = new List<Item>();

            fs.Seek(8, SeekOrigin.Begin);

            for (int i = 0; i < itemAmount; i++)
            {

                var item = new Item();

                item.Place = br.ReadInt32();

                item.CategoryIndex = br.ReadInt32();
                item.SubCategoryIndex = br.ReadInt32();
                item.Icon = Encoding.GetEncoding(936).GetString(br.ReadBytes(128)).Split('\0')[0];
                item.ID = br.ReadInt32();
                item.Amount = br.ReadInt32();
                item.Sales = new List<Sale>(Variables.MaxSalesPerItem);

                for (int g = 0; g < item.Sales.Capacity; g++)
                {
                    var sale = new Sale();

                    sale.Price = br.ReadInt32();
                    sale.SellingEndTime = br.ReadInt32();
                    sale.During = br.ReadInt32();
                    sale.SellingStartTime = br.ReadInt32();
                    sale.Control = br.ReadInt32();
                    sale.Day = br.ReadInt32();
                    sale.Status = br.ReadInt32();
                    if (sale.Status > 19) sale.Status = 0;
                    sale.Flags = br.ReadInt32();
                    sale.VipLevelRequired = br.ReadInt32();
                    sale.Control = g == 0 ? -1 : 0;
                    item.Sales.Add(sale);
                }

                item.Explanation = Encoding.Unicode.GetString(br.ReadBytes(1024)).TrimEnd('\0');
                item.Name = Encoding.Unicode.GetString(br.ReadBytes(64)).Split('\0')[0];

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