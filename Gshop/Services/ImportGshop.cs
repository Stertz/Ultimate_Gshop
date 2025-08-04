using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultimate_Gshop.Gshop.Services
{
    public static class ImportGshop
    {
        public static Shop Handle(string gshopFilepath, GshopSettings gshopSettings)
        {
            if (!File.Exists(gshopFilepath)) throw new FileNotFoundException(gshopFilepath);
            File.Copy(gshopFilepath, gshopSettings.TemporaryGshop);
            var gshop = new Shop();
            LoadGshop.TryLoadGshop(gshop, gshopSettings);
            return gshop;
        }
    }
}
