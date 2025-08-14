using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ultimate_Gshop.Views;

namespace Ultimate_Gshop.Misc
{
    public static class SetGshopFileFromOS
    {
        public static async Task<string?> Handle(Window window)
        {
            var options = new FolderPickerOpenOptions
            {
                Title = "Select folder to release file",
                AllowMultiple = false
            };

            var folders = await window.StorageProvider.OpenFolderPickerAsync(options);

            var folder = folders.ToList().FirstOrDefault();

            return folder?.TryGetLocalPath();
        }
    }
}