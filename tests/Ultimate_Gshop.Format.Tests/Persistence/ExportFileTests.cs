using Ultimate_Gshop.Gshop.Services;

namespace Ultimate_Gshop.Format.Tests;

public sealed class ExportFileTests
{
    [Theory]
    [InlineData(GshopExportTarget.Client)]
    [InlineData(GshopExportTarget.Server)]
    public void ExportRoutesToTheRequestedBinaryLayoutAndTruncatesOldOutput(GshopExportTarget target)
    {
        using var directory = new TestDirectory();
        var path = Path.Combine(directory.Path, "output.data");
        File.WriteAllBytes(path, new byte[20000]);
        var shop = ShopFixture.Create(2);

        ExportGshopFile.Handle(shop, path, target);

        var expected = ShopFixture.Export(shop, server: target == GshopExportTarget.Server);
        Assert.Equal(expected, File.ReadAllBytes(path));
        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*.tmp"));
    }

    [Theory]
    [InlineData(GshopExportTarget.Client)]
    [InlineData(GshopExportTarget.Server)]
    public void SerializationFailureDoesNotDestroyThePreviousExport(GshopExportTarget target)
    {
        using var directory = new TestDirectory();
        var path = Path.Combine(directory.Path, "output.data");
        byte[] original = [1, 2, 3, 4];
        File.WriteAllBytes(path, original);
        var shop = ShopFixture.Create(1);
        shop.Itens[0].Sales.Clear();

        Assert.Throws<ArgumentOutOfRangeException>(() => ExportGshopFile.Handle(shop, path, target));

        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public void AnInvalidTargetCannotCreateAnOutputFile()
    {
        using var directory = new TestDirectory();
        var path = Path.Combine(directory.Path, "output.data");
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExportGshopFile.Handle(ShopFixture.Create(), path, (GshopExportTarget)99));
        Assert.Empty(Directory.EnumerateFiles(directory.Path));
    }
}
