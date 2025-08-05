using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace Ultimate_Gshop.Gshop.Services
{
    public static class ExportGshopServer
    {
        public static void Handle(BinaryWriter bw, Shop shop)
        {
            var itens = shop.Itens;
            bw.Write((Int32)(shop.Timestamp.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
            bw.Write(shop.ItemAmount);
            for (int i = 0; i < shop.ItemAmount; i++)
            {
                bw.Write(itens[i].Place);
                bw.Write(itens[i].CategoryIndex);
                bw.Write(itens[i].SubCategoryIndex);
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
        }
    }
}
