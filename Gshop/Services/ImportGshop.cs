using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Gshop.Services
{
    public static class ImportGshop
    {
        public static async Task<Shop> Handle(Window window)
        {

            IStorageFile? file = await GetGshopFileFromOS.Handle(window);

            if (file is null) return new Shop();

            var filepath = file.TryGetLocalPath() ?? throw new InvalidOperationException("Invalid path");

            File.Copy(filepath, ApplicationSettings.GshopLocation, true);

            return LoadGshop.Handle(ApplicationSettings.GshopLocation);

        }
    }
}
