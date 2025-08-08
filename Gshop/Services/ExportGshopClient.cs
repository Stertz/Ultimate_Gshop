using System.Text;
using Ultimate_Gshop.Gshop.Misc;
namespace Ultimate_Gshop.Gshop.Services
{
    public static class ExportGshopClient
    {
        public static void Handle(BinaryWriter bw, Shop shop)
        {

            var itens = shop.Itens;
            var categories = shop.Categories;

            bw.Write((Int32)(shop.Timestamp.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
            bw.Write(shop.ItemAmount);
            for (int i = 0; i < shop.ItemAmount; i++)
            {
                bw.Write(itens[i].Place);
                bw.Write(itens[i].CategoryIndex);
                bw.Write(itens[i].SubCategoryIndex);
                bw.Write(BytesHelper.GetBytes(itens[i].Icon, 128, Encoding.GetEncoding(936)));
                bw.Write(itens[i].ID);
                bw.Write(itens[i].Amount);
                for (int f = 0; f < 4; f++)
                {
                    bw.Write(itens[i].Sales[f].Price);
                    bw.Write(itens[i].Sales[f].SellingEndTime);
                    bw.Write(itens[i].Sales[f].During);
                    bw.Write(itens[i].Sales[f].SellingStartTime);
                    bw.Write(itens[i].Sales[f].Control);
                    bw.Write(itens[i].Sales[f].Day);
                    bw.Write(itens[i].Sales[f].Status);
                    bw.Write(itens[i].Sales[f].Flags);
                    bw.Write(itens[i].Sales[f].VipLevelRequired);
                }
                bw.Write(BytesHelper.GetBytes(itens[i].Explanation, 1024, Encoding.Unicode));
                bw.Write(BytesHelper.GetBytes(itens[i].Name, 64, Encoding.Unicode));
                bw.Write(itens[i].GiftID);
                bw.Write(itens[i].GiftAmount);
                bw.Write(itens[i].GiftTime);
                bw.Write(itens[i].ILogPrice);
                for (int f = 0; f < 8; f++)
                {
                    bw.Write(itens[i].OwnerNpcs[f]);
                }
                bw.Write(itens[i].LimitPeriod);
                bw.Write(itens[i].AvailFrequency);
                bw.Write(itens[i].Reserve1);
                bw.Write(itens[i].Reserve2);
                bw.Write(itens[i].Class);
                bw.Write(itens[i].Reserve3);
                bw.Write(itens[i].Reserve4);
                bw.Write(itens[i].Reserve5);
                bw.Write(itens[i].Reserve6);
            }
            for (int d = 0; d < 8; d++)
            {
                bw.Write(BytesHelper.GetBytes(categories[d].Title, 128, Encoding.Unicode));
                bw.Write(categories[d].SubCategories.Count);
                for (int s = 0; s < categories[d].SubCategories.Count; s++)
                {
                    bw.Write(categories[d].SubCategories[s].GetBytesFromString(128, Encoding.Unicode));
                }
            }
        }
    }
}