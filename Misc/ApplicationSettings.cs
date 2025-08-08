using Avalonia;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Ultimate_Gshop.Gshop;

namespace Ultimate_Gshop.Misc
{
    public static class ApplicationSettings
    {
        public static string AppFolder { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ultimate_gshop");

        private static readonly string _settings = Path.Combine(AppFolder, "settings.json");

        public static string GshopTemp { get; } = Path.Combine(AppFolder, "gshop_temp.data");

        public static JsonSerializerOptions IdentedJson { get; } = new() { WriteIndented = true };

        public static GshopSettings Load()
        {
            if (!File.Exists(_settings))
                return new GshopSettings();

            var json = File.ReadAllText(_settings);
            return JsonSerializer.Deserialize<GshopSettings>(json) ?? new GshopSettings();
        }

        public static void Save(GshopSettings settings)
        {
            Directory.CreateDirectory(AppFolder);
            var json = JsonSerializer.Serialize(settings, IdentedJson);
            File.WriteAllText(_settings, json);
        }

        public static bool GshopExists => File.Exists(GshopTemp);
        public static string GshopLocation { get; } = GshopTemp;

        /// <summary>
        /// Define cursor personalizado
        /// </summary>
        public static Cursor InitializeCustomCursor()
        {

            using var stream = AssetLoader.Open(new Uri("avares://Ultimate_Gshop/Resources/cursor.png"));
            var bitmap = new Bitmap(stream);
            return new Cursor(bitmap, new PixelPoint(0, 0));
        }
    }
}
