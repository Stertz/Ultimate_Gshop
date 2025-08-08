using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Gshop.Services
{
    public static class SaveGshop
    {
        public static void Handle(Shop shop, ShopTracking tracking)
        {
            ShopTracking.Save(tracking);
            using var bw = new BinaryWriter(File.OpenWrite(ApplicationSettings.GshopTemp));
            ExportGshopClient.Handle(bw, shop);
        }
    }
}
