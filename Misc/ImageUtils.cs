using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Collections.Concurrent;

namespace Ultimate_Gshop.Misc
{
    public static class ImageUtils
    {
        private static readonly ConcurrentDictionary<string, Bitmap> _cache = new();

        private static Bitmap LoadFromDiskOrFallback(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new Exception("Empty image path.");

                if (path.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                    path = Path.ChangeExtension(path, ".png");

                var fullPath = Path.Combine(AppContext.BaseDirectory, "resources", path.TrimStart('/', '\\'));

                if (!File.Exists(fullPath))
                    throw new FileNotFoundException($"Image not found: {fullPath}");

                using var fileStream = File.OpenRead(fullPath);
                return new Bitmap(fileStream);
            }
            catch
            {
                using var fallback = AssetLoader.Open(new Uri("avares://Ultimate_Gshop/Resources/question_mark.png"));
                return new Bitmap(fallback);
            }
        }

        public static Bitmap LoadImage(string iconPath)
        {
            return _cache.GetOrAdd(iconPath, LoadFromDiskOrFallback);
        }

        public static Bitmap LoadImageReduced(string iconPath, int width = 32, int height = 32)
        {
            string cacheKey = $"{iconPath}_{width}x{height}";

            return _cache.GetOrAdd(cacheKey, _ =>
            {
                using var original = LoadImage(iconPath);
                return original.CreateScaledBitmap(new Avalonia.PixelSize(width, height));
            });
        }
    }
}
