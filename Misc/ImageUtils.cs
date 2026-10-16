using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Collections.Concurrent;

namespace Ultimate_Gshop.Misc
{
    public static class ImageUtils
    {
        private const string FallbackKey = "<embedded-fallback>";
        private static readonly StringComparer PathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        private static readonly ConcurrentDictionary<string, Lazy<Bitmap>> _cacheOriginal = new(PathComparer);
        private static readonly ConcurrentDictionary<string, Lazy<Bitmap>> _cacheReduced = new(PathComparer);
        internal static (int Originals, int Reduced) CacheEntryCounts => (_cacheOriginal.Count, _cacheReduced.Count);

        private static string CacheKey(string iconPath)
        {
            if (string.IsNullOrWhiteSpace(iconPath)) return FallbackKey;
            var path = ResolveImagePath(iconPath);
            return File.Exists(path) ? Path.GetFullPath(path) : FallbackKey;
        }

        private static Stream OpenStreamOrFallback(string path)
        {
            try
            {
                if (path != FallbackKey) return File.OpenRead(path);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                // A resource can disappear between resolving it and opening it.
            }
            return AssetLoader.Open(new Uri("avares://Ultimate_Gshop/Resources/question_mark.png"));
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
            return LoadOriginal(CacheKey(iconPath));
        }

        // Cache owns these images until application exit; consumers must not dispose them.
        private static Bitmap LoadOriginal(string key) => _cacheOriginal.GetOrAdd(key, static path => new Lazy<Bitmap>(() =>
        {
            using var stream = OpenStreamOrFallback(path);
            return new Bitmap(stream);
        })).Value;

        public static Bitmap LoadImageReduced(string iconPath, int width = 32, int height = 32)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
            var originalKey = CacheKey(iconPath);
            var cacheKey = $"{originalKey}|{width}x{height}";
            return _cacheReduced.GetOrAdd(cacheKey, _ => new Lazy<Bitmap>(() =>
            {
                var original = LoadOriginal(originalKey);
                return original.CreateScaledBitmap(new Avalonia.PixelSize(width, height));
            })).Value;
        }

        // Call only after all views close; disposing a shared image during eviction is unsafe.
        internal static void ReleaseResources()
        {
            foreach (var image in _cacheReduced.Values)
                if (image.IsValueCreated) image.Value.Dispose();
            _cacheReduced.Clear();
            foreach (var image in _cacheOriginal.Values)
                if (image.IsValueCreated) image.Value.Dispose();
            _cacheOriginal.Clear();
        }
    }
}
