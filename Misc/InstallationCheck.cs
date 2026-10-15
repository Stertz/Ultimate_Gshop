using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;

namespace Ultimate_Gshop.Misc;

internal static class InstallationCheck
{
    internal static void Run()
    {
        // Exercise installed managed/native dependencies without a display or the user's session.
        AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem()
            .UseWindowingSubsystem(static () => { }, "Installation verification")
            .UseSkia().UseHarfBuzz().SetupWithoutStarting();
        var surfaces = Path.Combine(AppContext.BaseDirectory, "Resources", "surfaces");
        var imagePath = Directory.EnumerateFiles(surfaces, "*.png", SearchOption.AllDirectories).First();
        using (var stream = File.OpenRead(imagePath))
        using (var image = new Bitmap(stream))
        {
            if (image.PixelSize.Width <= 0 || image.PixelSize.Height <= 0)
                throw new InvalidDataException("A bundled game image could not be decoded.");
        }
        using (var stream = AssetLoader.Open(new Uri("avares://Ultimate_Gshop/Resources/question_mark.png")))
        using (var image = new Bitmap(stream))
        {
            if (image.PixelSize.Width <= 0 || image.PixelSize.Height <= 0)
                throw new InvalidDataException("The embedded fallback image could not be decoded.");
        }

        var temporary = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"ultimate-gshop-installation-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(temporary);
        try
        {
            var shop = NewEmptyGshop.Handle();
            SaveGshop.Handle(shop, new ShopTracking(), new GshopSettings(), temporary);
            var clientPath = Path.Combine(temporary, "gshop.data");
            var original = File.ReadAllBytes(clientPath);
            var loaded = LoadGshop.Handle(clientPath);
            ExportGshopFile.Handle(loaded, clientPath, GshopExportTarget.Client);
            if (!original.SequenceEqual(File.ReadAllBytes(clientPath)))
                throw new InvalidDataException("The installed client writer failed its synthetic round trip.");
            var serverPath = Path.Combine(temporary, "gshopsev.data");
            ExportGshopFile.Handle(loaded, serverPath, GshopExportTarget.Server);
            if (new FileInfo(serverPath).Length != 8 + loaded.Itens.Count * 248L)
                throw new InvalidDataException("The installed server writer produced an unexpected record size.");
        }
        finally
        {
            var relative = Path.GetRelativePath(Path.GetTempPath(), temporary);
            if (!relative.StartsWith("ultimate-gshop-installation-", StringComparison.Ordinal) ||
                relative != Path.GetFileName(temporary))
                throw new InvalidOperationException("Unexpected installation-check cleanup path.");
            Directory.Delete(temporary, recursive: true);
        }
        Console.WriteLine("Installation verification passed: resources, native raster decoding, synthetic save/load, and client/server writers.");
    }
}
