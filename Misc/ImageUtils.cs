using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Collections.Concurrent;

namespace Ultimate_Gshop.Misc
{
    public static class ImageUtils
    {
        private static readonly ConcurrentDictionary<string, Bitmap> _cacheOriginal = new();
        private static readonly ConcurrentDictionary<string, Bitmap> _cacheReduced = new();

        private static Stream OpenStreamOrFallback(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new Exception("Empty image path.");

                path = ResolveImagePath(path);

                if (!File.Exists(path))
                    throw new FileNotFoundException($"Image not found: {path}");

                return File.OpenRead(path); // caller fecha
            }
            catch
            {
                // fallback embutido
                return AssetLoader.Open(new Uri("avares://Ultimate_Gshop/Resources/question_mark.png"));
            }
        }

        internal static string ResolveImagePath(string path)
        {
            if (path.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                path = Path.ChangeExtension(path, ".png");

            var relative = path.TrimStart('/', '\\')
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(AppContext.BaseDirectory, "Resources", relative);
        }

        public static Bitmap LoadImage(string iconPath)
        {
            // Nunca descarte os bitmaps que vão para este cache
            return _cacheOriginal.GetOrAdd(iconPath, key =>
            {
                using var s = OpenStreamOrFallback(key);
                return new Bitmap(s);
            });
        }

        public static Bitmap LoadImageReduced(string iconPath, int width = 32, int height = 32)
        {
            var cacheKey = $"{iconPath}|{width}x{height}";
            return _cacheReduced.GetOrAdd(cacheKey, _ =>
            {
                var original = LoadImage(iconPath); // vem do _cacheOriginal
                return original.CreateScaledBitmap(new Avalonia.PixelSize(width, height));
            });
        }
    }
}
