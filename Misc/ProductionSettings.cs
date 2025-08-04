using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Ultimate_Gshop.Gshop;

namespace Ultimate_Gshop.Misc
{
    public static class ProductionSettings
    {
        private static readonly string _folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ultimate_gshop");

        private static readonly string _settings = Path.Combine(_folder, "settings.json");

        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public static GshopSettings Load()
        {
            if (!File.Exists(_settings))
                return new GshopSettings();

            var json = File.ReadAllText(_settings);
            return JsonSerializer.Deserialize<GshopSettings>(json) ?? new GshopSettings();
        }

        public static void Save(GshopSettings settings)
        {
            Directory.CreateDirectory(_folder);
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(_settings, json);
        }

    }
}
