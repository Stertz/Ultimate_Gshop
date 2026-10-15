using Ultimate_Gshop.Misc;
namespace Ultimate_Gshop.Gshop.Services
{
    using System.Text;
    using System.Text.Json;
    using Ultimate_Gshop.Gshop.Misc;
    public static class SaveGshop
    {
        public static void Handle(Shop shop, ShopTracking tracking, GshopSettings settings)
        {
            WriteSession(shop, tracking, settings, ApplicationSettings.AppFolder, "gshop_temp.data");
        }

        public static void Handle(Shop shop, ShopTracking tracking, GshopSettings settings, string path)
        {
            WriteSession(shop, tracking, settings, path, "gshop.data");
        }

        private static void WriteSession(Shop shop, ShopTracking tracking, GshopSettings settings,
            string directory, string binaryName) => SessionTransaction.Write(directory,
                new Dictionary<string, Action<Stream>>
                {
                    [binaryName] = stream =>
                    {
                        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
                        ExportGshopClient.Handle(writer, shop);
                    },
                    ["tracking.json"] = stream => JsonSerializer.Serialize(stream, tracking, ApplicationSettings.IdentedJson),
                    ["settings.json"] = stream => JsonSerializer.Serialize(stream, settings, ApplicationSettings.IdentedJson)
                });

    }
}
