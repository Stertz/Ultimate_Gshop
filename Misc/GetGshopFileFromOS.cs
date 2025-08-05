using Avalonia.Controls;
using Avalonia.Platform.Storage;
namespace Ultimate_Gshop.Misc
{
    public static class GetGshopFileFromOS
    {
        public static async Task<IStorageFile?> Handle(Window window)
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Select Gshop file",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Data Files") { Patterns = ["*.data"] },
                    new FilePickerFileType("All Files") { Patterns = ["*.*"] }
                ]
            };

            var files = await window.StorageProvider.OpenFilePickerAsync(options);

            var file = files.ToList().FirstOrDefault();

            if (file is null)
            {
                var result = await new GameMessageBox("No valid file provided", "Try again?", "Yes", "No").ShowDialogAsync(window);
                if (result == "Sim")
                {
                   return await Handle(window);
                }
            }

            return file;

        }
    }
}