using Ultimate_Gshop.Misc;
namespace Ultimate_Gshop.Gshop.Services
{
    public static class SaveGshop
    {
        public static void Handle(Shop shop, ShopTracking tracking, GshopSettings settings)
        {
            using var bw = new BinaryWriter(File.OpenWrite(ApplicationSettings.GshopTemp));
            ExportGshopClient.Handle(bw, shop);
            ShopTracking.Save(tracking);
            GshopSettings.Save(settings);
        }

        public static void Handle(Shop shop, ShopTracking tracking, GshopSettings settings, string path)
        {
            using var bw = new BinaryWriter(File.OpenWrite(Path.Combine(path, "gshop.data")));
            ExportGshopClient.Handle(bw, shop);
            ShopTracking.Save(tracking, Path.Combine(path, "tracking.json"));
            GshopSettings.Save(settings, Path.Combine(path, "settings.json"));
        }


    }
}