using System.IO;
using Ultimate_Gshop.Categories.Services;
using Ultimate_Gshop.Itens.Services;
using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Gshop.Services
{
    internal static class LoadGshop
    {
        public static void TryLoadGshop(Shop gshop, GshopSettings gshopSettings)
        {

            var path = gshopSettings.TemporaryGshop;

            if (!File.Exists(path)) throw new InvalidOperationException("Gshop não encontrado");

            using var fs = File.Open(Variables.LastGshopPath, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);

            (gshop.Timestamp, gshop.ItemAmount) = GetHeader.Handle(br);

            gshop.Categories = [.. GetCategories.Handle(br, fs, gshop.ItemAmount)];

            gshop.CategoriesLoaded?.Invoke(null, new EventArgs());

            gshop.Itens = [.. GetItens.Handle(gshop.ItemAmount, br, fs)];

            gshop.ItensLoaded?.Invoke(null, new EventArgs());

        }
    }
}
