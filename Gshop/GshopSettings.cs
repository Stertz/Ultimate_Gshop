using System.Text.Json;
using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Gshop
{
    using Ultimate_Gshop.Gshop.Misc;
    public class GshopSettings
    {
        public string? LastGshopLoadedFrom { get; set; }
        public string? LastBackupFrom { get; set; }
        public bool AutoSave { get; set; }
        public int? SaveFromSeconds { get; set; }

        private static readonly string _settings = Path.Combine(ApplicationSettings.AppFolder, "settings.json");

        public static GshopSettings Load() => Load(_settings);

        public static GshopSettings Load(string path) => SessionTransaction.Read(Path.GetDirectoryName(Path.GetFullPath(path))!, () =>
            File.Exists(path) ? JsonSerializer.Deserialize<GshopSettings>(File.ReadAllText(path)) ?? new GshopSettings()
                : new GshopSettings());

        public static void Save(GshopSettings settings)
        {
            Directory.CreateDirectory(ApplicationSettings.AppFolder);
            Save(settings, _settings);
        }

        public static void Save(GshopSettings settings, string path)
        {
            SessionTransaction.Read(Path.GetDirectoryName(Path.GetFullPath(path))!, () =>
            {
                ProtectedFile.Write(path, stream => JsonSerializer.Serialize(stream, settings, ApplicationSettings.IdentedJson));
                return true;
            });
        }

    }
}
