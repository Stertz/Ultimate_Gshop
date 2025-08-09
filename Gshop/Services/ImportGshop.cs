using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Ultimate_Gshop.Misc;
namespace Ultimate_Gshop.Gshop.Services
{
    public static class ImportGshop
    {
        public static async Task<Shop?> Handle(Window window)
        {

            IStorageFile? file = await GetGshopFileFromOS.Handle(window);

            if (file is null) return null;

            var filepath = file.TryGetLocalPath() ?? throw new InvalidOperationException("Invalid path");

            Directory.CreateDirectory(Path.GetDirectoryName(ApplicationSettings.GshopBefore)!);
            Directory.CreateDirectory(Path.GetDirectoryName(ApplicationSettings.GshopTemp)!);

            File.Copy(filepath, ApplicationSettings.GshopBefore, true);
            File.Copy(filepath, ApplicationSettings.GshopTemp, true);

            return LoadGshop.Handle(ApplicationSettings.GshopTemp);

        }
    }
}