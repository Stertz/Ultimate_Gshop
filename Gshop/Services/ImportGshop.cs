using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Ultimate_Gshop.Gshop.Misc;
using Ultimate_Gshop.Misc;
namespace Ultimate_Gshop.Gshop.Services
{
    public static class ImportGshop
    {
        public static async Task<Shop?> Handle(Window window, GshopSettings? settings = null)
        {

            IStorageFile? file = await GetGshopFileFromOS.Handle(window);

            if (file is null) return null;

            var filepath = file.TryGetLocalPath() ?? throw new InvalidOperationException("Invalid path");
            return Handle(filepath, ApplicationSettings.AppFolder, settings);
        }

        internal static Shop Handle(string filepath, string sessionDirectory, GshopSettings? settings = null)
        {
            // Restore an interrupted generation before taking a snapshot of an existing session file.
            SessionTransaction.Read(sessionDirectory, () => true);

            var snapshot = Path.Combine(sessionDirectory, $".import.{Guid.NewGuid():N}.tmp");
            var ownsSnapshot = false;
            try
            {
                using (var input = File.OpenRead(filepath))
                using (var output = new FileStream(snapshot, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    ownsSnapshot = true;
                    input.CopyTo(output);
                }

                // Validate the same snapshot that will be committed, retaining its original bytes.
                var shop = LoadGshop.Handle(snapshot);
                settings ??= GshopSettings.Load(Path.Combine(sessionDirectory, "settings.json"));
                StartGshopSession.Handle(shop, settings, sessionDirectory, filepath, snapshot);
                return shop;
            }
            finally
            {
                if (ownsSnapshot && File.Exists(snapshot))
                    File.Delete(snapshot);
            }

        }
    }
}
