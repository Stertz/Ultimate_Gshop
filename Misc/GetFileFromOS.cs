using Avalonia.Controls;
using Avalonia.Platform.Storage;
namespace Ultimate_Gshop.Misc
{
    public static class GetFileFromOS
    {
        public static async Task<IStorageFile?> ShowOpenFileDialog(Window window)
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

            return files.ToList().FirstOrDefault();

        }
    }
}