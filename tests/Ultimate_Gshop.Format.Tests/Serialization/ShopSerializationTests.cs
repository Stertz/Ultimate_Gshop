using System.Buffers.Binary;
using System.Text;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Itens;

namespace Ultimate_Gshop.Format.Tests;

public sealed class ShopSerializationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExportUsesTheActualRecordCountAfterAnUndo(bool server)
    {
        var shop = ShopFixture.Create(1);
        var history = new Ultimate_Gshop.Gshop.ShopTracking();
        Ultimate_Gshop.Itens.Services.InsertItem.Handle(shop.Itens, ShopFixture.CreateItem(1), history);
        shop.ItemAmount = 2;
        history.Undo(shop);

        var bytes = ShopFixture.Export(shop, server);

        Assert.Single(shop.Itens);
        Assert.Equal(1, ReadInt(bytes, 4));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AStaleHeaderCountCannotSilentlyOmitAnExistingRecord(bool server)
    {
        var shop = ShopFixture.Create(2);
        shop.ItemAmount = 1;

        var bytes = ShopFixture.Export(shop, server);

        Assert.Equal(2, ReadInt(bytes, 4));
        Assert.Equal(1001, ReadInt(bytes, 8 + (server ? 248 : 1464) + (server ? 12 : 140)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ClientExportKeepsHeaderRecordSizesTextEncodingsAndCategoryOffsets(int itemCount)
    {
        var shop = ShopFixture.Create(itemCount);
        var bytes = ShopFixture.Export(shop);

        Assert.Equal(8 + itemCount * 1464 + 8 * (128 + 4 + 128), bytes.Length);
        Assert.Equal(1704067200, ReadInt(bytes, 0));
        Assert.Equal(itemCount, ReadInt(bytes, 4));

        for (var index = 0; index < itemCount; index++)
        {
            var item = shop.Itens[index];
            var offset = 8 + index * 1464;
            AssertRecord(bytes, offset, item, server: false);
            AssertPaddedText(bytes, offset + 12, 128, item.Icon, Encoding.GetEncoding(936));
            AssertPaddedText(bytes, offset + 292, 1024, item.Explanation, Encoding.Unicode);
            AssertPaddedText(bytes, offset + 1316, 64, item.Name, Encoding.Unicode);
        }

        var categoryOffset = 8 + itemCount * 1464;
        for (var index = 0; index < 8; index++)
        {
            AssertPaddedText(bytes, categoryOffset, 128, $"Category {index}", Encoding.Unicode);
            Assert.Equal(1, ReadInt(bytes, categoryOffset + 128));
            AssertPaddedText(bytes, categoryOffset + 132, 128, $"Subcategory {index}", Encoding.Unicode);
            categoryOffset += 260;
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ServerExportKeepsItsDistinctRecordLayoutWithoutClientTextOrCategoryBlocks(int itemCount)
    {
        var shop = ShopFixture.Create(itemCount);
        var bytes = ShopFixture.Export(shop, server: true);

        Assert.Equal(8 + itemCount * 248, bytes.Length);
        Assert.Equal(1704067200, ReadInt(bytes, 0));
        Assert.Equal(itemCount, ReadInt(bytes, 4));
        for (var index = 0; index < itemCount; index++)
            AssertRecord(bytes, 8 + index * 248, shop.Itens[index], server: true);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SupportedSyntheticClientDataRoundTripsWithoutByteChanges(int itemCount)
    {
        var original = ShopFixture.Export(ShopFixture.Create(itemCount));
        using var directory = new TestDirectory();
        var path = Path.Combine(directory.Path, "gshop.data");
        File.WriteAllBytes(path, original);

        var loaded = LoadGshop.Handle(path);

        Assert.Equal(itemCount, loaded.ItemAmount);
        Assert.Equal(itemCount, loaded.Itens.Count);
        Assert.Equal(original, ShopFixture.Export(loaded));
    }

    [Fact]
    public void LoaderCharacterizesLegacyPlaceControlAndUnsupportedStatusNormalization()
    {
        // R03 remains a format-compatibility decision; this test records the current behavior.
        var shop = ShopFixture.Create(1);
        shop.Itens[0].Place = 42;
        shop.Itens[0].Sales[0].Control = 77;
        shop.Itens[0].Sales[0].Status = 20;
        using var directory = new TestDirectory();
        var path = Path.Combine(directory.Path, "gshop.data");
        File.WriteAllBytes(path, ShopFixture.Export(shop));

        var item = Assert.Single(LoadGshop.Handle(path).Itens);

        Assert.Equal(0, item.Place);
        Assert.Equal(-1, item.Sales[0].Control);
        Assert.Equal(0, item.Sales[0].Status);
        Assert.Equal(@"surfaces\百宝阁\中文.dds", item.Icon);
        Assert.Equal(17, item.Reserve6);
    }

    private static void AssertRecord(byte[] bytes, int offset, Item item, bool server)
    {
        Assert.Equal(item.Place, ReadInt(bytes, offset));
        Assert.Equal(item.CategoryIndex, ReadInt(bytes, offset + 4));
        Assert.Equal(item.SubCategoryIndex, ReadInt(bytes, offset + 8));
        Assert.Equal(item.ID, ReadInt(bytes, offset + (server ? 12 : 140)));
        Assert.Equal(item.Amount, ReadInt(bytes, offset + (server ? 16 : 144)));

        var saleStart = offset + (server ? 20 : 148);
        for (var slot = 0; slot < 4; slot++)
        {
            var sale = item.Sales[slot];
            int[] fields = [sale.Price, sale.SellingEndTime, sale.During, sale.SellingStartTime,
                sale.Control, sale.Day, sale.Status, sale.Flags, sale.VipLevelRequired];
            for (var field = 0; field < fields.Length; field++)
                Assert.Equal(fields[field], ReadInt(bytes, saleStart + slot * 36 + field * 4));
        }

        var giftStart = offset + (server ? 164 : 1380);
        int[] gift = [item.GiftID, item.GiftAmount, item.GiftTime, item.ILogPrice];
        for (var field = 0; field < gift.Length; field++)
            Assert.Equal(gift[field], ReadInt(bytes, giftStart + field * 4));
        for (var owner = 0; owner < 8; owner++)
            Assert.Equal(item.OwnerNpcs[owner], ReadInt(bytes, giftStart + 16 + owner * 4));

        var tailStart = offset + (server ? 212 : 1428);
        int[] tail = [item.LimitPeriod, item.AvailFrequency, item.Reserve1, item.Reserve2,
            item.Class, item.Reserve3, item.Reserve4, item.Reserve5, item.Reserve6];
        for (var field = 0; field < tail.Length; field++)
            Assert.Equal(tail[field], ReadInt(bytes, tailStart + field * 4));
    }

    private static int ReadInt(byte[] bytes, int offset) =>
        BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset, 4));

    private static void AssertPaddedText(byte[] bytes, int offset, int width, string text, Encoding encoding)
    {
        var expected = new byte[width];
        encoding.GetBytes(text).CopyTo(expected, 0);
        Assert.Equal(expected, bytes.AsSpan(offset, width).ToArray());
    }
}
