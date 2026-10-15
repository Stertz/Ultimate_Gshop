using System.Text;
using System.Text.Json;
using Ultimate_Gshop.Gshop.Misc;
using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Gshop.Services;

internal static class StartGshopSession
{
    internal static void Handle(Shop shop, GshopSettings settings, string directory,
        string? sourcePath = null, string? snapshotPath = null)
    {
        var nextSettings = new GshopSettings
        {
            AutoSave = settings.AutoSave,
            SaveFromSeconds = settings.SaveFromSeconds,
            LastBackupFrom = settings.LastBackupFrom,
            LastGshopLoadedFrom = sourcePath
        };
        void WriteBinary(Stream stream)
        {
            if (snapshotPath is not null)
            {
                using var input = File.OpenRead(snapshotPath);
                input.CopyTo(stream);
            }
            else
            {
                using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
                ExportGshopClient.Handle(writer, shop);
            }
        }

        SessionTransaction.Write(directory, new Dictionary<string, Action<Stream>>
        {
            ["gshop_before.data"] = WriteBinary,
            ["gshop_temp.data"] = WriteBinary,
            ["tracking.json"] = stream => JsonSerializer.Serialize(stream, new ShopTracking(), ApplicationSettings.IdentedJson),
            ["settings.json"] = stream => JsonSerializer.Serialize(stream, nextSettings, ApplicationSettings.IdentedJson)
        });
        settings.LastGshopLoadedFrom = sourcePath;
    }
}
