using System.Text.Json;
using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Gshop
{
    public class GshopSettings
    {
        public string? LastGshopLoadedFrom { get; set; }
        public string? LastBackupFrom { get; set; }
        public bool AutoSave { get; set; }
        public int? SaveFromSeconds { get; set; }

        private static readonly string _settings = Path.Combine(ApplicationSettings.AppFolder, "settings.json");

        public static GshopSettings Load()
        {
            if (!File.Exists(_settings))
                return new GshopSettings();

            var json = File.ReadAllText(_settings);
            return JsonSerializer.Deserialize<GshopSettings>(json) ?? new GshopSettings();
        }

        public static void Save(GshopSettings settings)
        {
            Directory.CreateDirectory(ApplicationSettings.AppFolder);
            var json = JsonSerializer.Serialize(settings, ApplicationSettings.IdentedJson);
            File.WriteAllText(_settings, json);
        }

        public static void Save(GshopSettings settings, string path)
        {
            var json = JsonSerializer.Serialize(settings, ApplicationSettings.IdentedJson);
            File.WriteAllText(path, json);
        }

    }
}
