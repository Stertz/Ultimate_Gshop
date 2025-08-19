using Avalonia;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Text.Json;

namespace Ultimate_Gshop.Misc
{
    public static class ApplicationSettings
    {
        public static string AppFolder { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ultimate_gshop");

        public static string GshopBefore { get; } = Path.Combine(AppFolder, "gshop_before.data");
        public static string GshopTemp { get; } = Path.Combine(AppFolder, "gshop_temp.data");
        public static string BackupFolder { get; } = Path.Combine(AppFolder, "backups");

        public static JsonSerializerOptions IdentedJson { get; } = new() { WriteIndented = true };

        public static bool GshopExists => File.Exists(GshopTemp);

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
