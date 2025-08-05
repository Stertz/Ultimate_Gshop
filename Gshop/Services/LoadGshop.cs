using System.IO;
using Ultimate_Gshop.Categories.Services;
using Ultimate_Gshop.Itens.Services;
using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Gshop.Services
{
    internal static class LoadGshop
    {
        public static Shop Handle(string path)
        {

            if (!File.Exists(path)) throw new InvalidOperationException("Gshop não encontrado");

            var gshop = new Shop();

            using var fs = File.Open(path, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);

            (gshop.Timestamp, gshop.ItemAmount) = GetHeader.Handle(br);

            gshop.Categories = [.. GetCategories.Handle(br, fs, gshop.ItemAmount)];

            gshop.CategoriesLoaded?.Invoke(null, new EventArgs());

            gshop.Itens = [.. GetItens.Handle(gshop.ItemAmount, br, fs)];

            gshop.ItensLoaded?.Invoke(null, new EventArgs());

            return gshop;

        }
    }
}
