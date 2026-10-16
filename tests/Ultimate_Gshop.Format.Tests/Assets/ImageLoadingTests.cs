using System.Buffers.Binary;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Format.Tests;

[CollectionDefinition("Image rendering", DisableParallelization = true)]
public sealed class ImageRenderingCollection : ICollectionFixture<ImageRenderingFixture>;

public sealed class ImageRenderingFixture : IDisposable
{
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Thread _uiThread;

    public ImageRenderingFixture()
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _uiThread = new Thread(() =>
        {
            try
            {
                AppBuilder.Configure<App>()
                    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
                    .UseSkia().UseHarfBuzz().SetupWithoutStarting();
                ready.SetResult();
                Dispatcher.UIThread.MainLoop(_shutdown.Token);
            }
            catch (Exception error) { ready.TrySetException(error); }
        }) { IsBackground = true, Name = "Avalonia headless tests" };
        _uiThread.Start();
        ready.Task.GetAwaiter().GetResult();
    }

    public async Task Run(Action test) => await Dispatcher.UIThread.InvokeAsync(test);
    public async Task Run(Func<Task> test) => await Dispatcher.UIThread.InvokeAsync(test);

    public void Dispose()
    {
        _shutdown.Cancel();
        if (!_uiThread.Join(TimeSpan.FromSeconds(5)))
            throw new TimeoutException("The headless UI thread did not stop.");
        _shutdown.Dispose();
    }
}

[Collection("Image rendering")]
public sealed class ImageLoadingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExistingGameImagesLoadFromCopiedResourcesAndRetainTheirDimensions(bool windowsSeparators)
    {
        var fallbackSize = ImageUtils.LoadImage($"missing-{Guid.NewGuid():N}.png").PixelSize;
        var resourceRoot = Path.Combine(AppContext.BaseDirectory, "Resources");
        var sample = Directory.EnumerateFiles(Path.Combine(resourceRoot, "surfaces"), "*.png", SearchOption.AllDirectories)
            .Select(path => (Path: path, Size: ReadPngSize(path)))
            .First(entry => entry.Size != fallbackSize);
        var input = Path.ChangeExtension(Path.GetRelativePath(resourceRoot, sample.Path), ".dds").Replace('\\', '/');
        if (windowsSeparators)
            input = input.Replace('/', '\\');

        var image = ImageUtils.LoadImage(input);
        var reduced = ImageUtils.LoadImageReduced(input, 17, 23);

        Assert.Equal(sample.Size, image.PixelSize);
        Assert.Equal(new PixelSize(17, 23), reduced.PixelSize);
        Assert.Same(image, ImageUtils.LoadImage(input));
        Assert.Same(reduced, ImageUtils.LoadImageReduced(input, 17, 23));
    }

    [Fact]
    public void MissingAliasesShareOneFallbackAndExistingPathAliasesShareOneImage()
    {
        var first = ImageUtils.LoadImage($"missing-{Guid.NewGuid():N}.dds");
        Assert.Same(first, ImageUtils.LoadImage($"missing-{Guid.NewGuid():N}.png"));
        Assert.Same(first, ImageUtils.LoadImage(" "));
        Assert.Same(ImageUtils.LoadImageReduced("missing-a.dds"), ImageUtils.LoadImageReduced("missing-b.dds"));
        var resourceRoot = Path.Combine(AppContext.BaseDirectory, "Resources");
        var path = Directory.EnumerateFiles(Path.Combine(resourceRoot, "surfaces"), "*.png", SearchOption.AllDirectories).First();
        var relative = Path.GetRelativePath(resourceRoot, path);
        var dds = Path.ChangeExtension(relative, ".dds").Replace('\\', '/');
        Assert.Same(ImageUtils.LoadImage(relative), ImageUtils.LoadImage(dds));
        Assert.Same(ImageUtils.LoadImageReduced(relative), ImageUtils.LoadImageReduced(dds));
    }

    [Fact]
    public void CacheOwnsImagesAndCanReleaseThemAfterViewsClose()
    {
        ImageUtils.LoadImageReduced("cache-lifecycle-missing.dds");
        Assert.True(ImageUtils.CacheEntryCounts.Originals > 0);
        Assert.True(ImageUtils.CacheEntryCounts.Reduced > 0);
        ImageUtils.ReleaseResources();
        Assert.Equal((0, 0), ImageUtils.CacheEntryCounts);
        Assert.True(ImageUtils.LoadImageReduced("cache-lifecycle-missing.dds").PixelSize.Width > 0);
    }

    [Theory]
    [InlineData(0, 32)]
    [InlineData(32, -1)]
    public void InvalidResizeDimensionsAreRejectedBeforeAddingCacheEntries(int width, int height)
    {
        var before = ImageUtils.CacheEntryCounts;
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageUtils.LoadImageReduced("", width, height));
        Assert.Equal(before, ImageUtils.CacheEntryCounts);
    }

    [Fact]
    public void MissingImagesUseTheExistingEmbeddedFallback()
    {
        using var stream = AssetLoader.Open(new Uri("avares://Ultimate_Gshop/Resources/question_mark.png"));
        using var expected = new Bitmap(stream);

        var actual = ImageUtils.LoadImage($"missing-{Guid.NewGuid():N}.dds");

        Assert.Equal(expected.PixelSize, actual.PixelSize);
    }

    private static PixelSize ReadPngSize(string path)
    {
        Span<byte> header = stackalloc byte[24];
        using var stream = File.OpenRead(path);
        stream.ReadExactly(header);
        return new PixelSize(BinaryPrimitives.ReadInt32BigEndian(header[16..20]),
            BinaryPrimitives.ReadInt32BigEndian(header[20..24]));
    }
}
