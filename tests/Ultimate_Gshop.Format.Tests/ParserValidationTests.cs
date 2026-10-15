using System.Buffers.Binary;
using System.Text;
using Ultimate_Gshop.Categories.Services;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Itens.Services;

namespace Ultimate_Gshop.Format.Tests;

public sealed class ParserValidationTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void NegativeHeaderCountsAreRejected(int count)
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), count);
        using var reader = new BinaryReader(new MemoryStream(bytes));
        Assert.Throws<InvalidDataException>(() => GetHeader.Handle(reader));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void RecordReadersRejectCountsOutsideTheAvailableFile(int count)
    {
        using var directory = new TestDirectory();
        var path = Path.Combine(directory.Path, "invalid.data");
        File.WriteAllBytes(path, ShopFixture.Export(ShopFixture.Create(0)));
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        Assert.Throws<InvalidDataException>(() => GetCategories.Handle(reader, stream, count).ToArray());
        Assert.Throws<InvalidDataException>(() => GetItens.Handle(count, reader, stream).ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(127)]
    public void PartialCategoryTitlesAreNeverAccepted(int size)
    {
        using var reader = new BinaryReader(new MemoryStream(new byte[size]));
        Assert.Throws<EndOfStreamException>(() => GetCategoryTitle.Handle(reader));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void InvalidSubcategoryCountsAreRejected(int count)
    {
        var bytes = ShopFixture.Export(ShopFixture.Create(0));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8 + 128), count);
        using var directory = new TestDirectory();
        var path = Path.Combine(directory.Path, "invalid.data");
        File.WriteAllBytes(path, bytes);
        Assert.Throws<InvalidDataException>(() => LoadGshop.Handle(path));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(100)]
    [InlineData(1471)]
    [InlineData(1600)]
    [InlineData(3543)]
    public void TruncatedClientFilesFailWithoutReturningAPartialShop(int size)
    {
        var bytes = ShopFixture.Export(ShopFixture.Create(1));
        using var directory = new TestDirectory();
        var path = Path.Combine(directory.Path, "truncated.data");
        File.WriteAllBytes(path, bytes.AsSpan(0, size).ToArray());
        var error = Record.Exception(() => LoadGshop.Handle(path));
        Assert.True(error is IOException or InvalidDataException, $"Unexpected error: {error}");
    }

    [Fact]
    public void TrailingBytesRemainAcceptedWithoutAnUnverifiedFormatPolicyChange()
    {
        using var directory = new TestDirectory();
        var path = Path.Combine(directory.Path, "extended.data");
        var bytes = ShopFixture.Export(ShopFixture.Create(1));
        File.WriteAllBytes(path, [.. bytes, 0xAA, 0xBB]);
        Assert.Equal(bytes, ShopFixture.Export(LoadGshop.Handle(path)));
    }

    [Fact]
    public void ASubcategoryCountAboveByteRangeDoesNotWrapTheLoopCounter()
    {
        var shop = ShopFixture.Create(0);
        shop.Categories[0].SubCategories = Enumerable.Range(0, 256).Select(i => $"Subcategory {i}").ToList();
        using var directory = new TestDirectory();
        var path = Path.Combine(directory.Path, "many-subcategories.data");
        var bytes = ShopFixture.Export(shop);
        File.WriteAllBytes(path, bytes);
        var loaded = LoadGshop.Handle(path);
        Assert.Equal(shop.Categories[0].SubCategories, loaded.Categories[0].SubCategories);
        Assert.Equal(bytes, ShopFixture.Export(loaded));
    }
}
