using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultimate_Gshop.Misc
{
    public static class Variables
    {
        public static string LastGshopPath { get; } = @"C:\gshop\gshopeditado.data";
        public static string GshopClient { get; } = @"C:\gshop\gshop.data";
        public static int BytesPerItem { get; } = 1464;
        public static int MaxSalesPerItem { get; } = 4;
    }
}
