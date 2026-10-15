using System.Text;
using Ultimate_Gshop.Categories;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Itens;

namespace Ultimate_Gshop.Format.Tests;

internal static class ShopFixture
{
    internal static Shop Create(int itemCount = 2)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return new Shop
        {
            Timestamp = new DateTime(2024, 1, 1),
            ItemAmount = itemCount,
            Categories = Enumerable.Range(0, 8).Select(index => new Category
            {
                Title = $"Category {index}",
                SubCategories = [$"Subcategory {index}"]
            }).ToList(),
            Itens = Enumerable.Range(0, itemCount).Select(CreateItem).ToList()
        };
    }

    internal static Item CreateItem(int index = 0) => new()
    {
        Place = index,
        CategoryIndex = index % 8,
        SubCategoryIndex = 0,
        ID = 1000 + index,
        Amount = 2 + index,
        Icon = @"surfaces\百宝阁\中文.dds",
        Name = $"中文 Item {index}",
        Explanation = "^FFFFFFExample description",
        Sales = Enumerable.Range(0, 4).Select(slot => new Sale
        {
            Price = 1000 + slot,
            During = 3600 + slot,
            Control = slot == 0 ? -1 : 0,
            Status = slot == 0 ? 1 : 0,
            Flags = 10 + slot,
            VipLevelRequired = slot
        }).ToList(),
        GiftID = 8000 + index,
        GiftAmount = 9,
        GiftTime = 10,
        ILogPrice = 12345,
        OwnerNpcs = Enumerable.Range(100, 8).ToArray(),
        LimitPeriod = 7,
        AvailFrequency = 8,
        Reserve1 = 11,
        Reserve2 = 12,
        Class = 13,
        Reserve3 = 14,
        Reserve4 = 15,
        Reserve5 = 16,
        Reserve6 = 17
    };

    internal static byte[] Export(Shop shop, bool server = false)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        if (server)
            ExportGshopServer.Handle(writer, shop);
        else
            ExportGshopClient.Handle(writer, shop);
        writer.Flush();
        return stream.ToArray();
    }
}

internal sealed class TestDirectory : IDisposable
{
    internal string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
        $"ultimate-gshop-tests-{Guid.NewGuid():N}");

    internal TestDirectory() => Directory.CreateDirectory(Path);

    public void Dispose()
    {
        var relative = System.IO.Path.GetRelativePath(System.IO.Path.GetTempPath(), Path);
        if (!relative.StartsWith("ultimate-gshop-tests-", StringComparison.Ordinal) ||
            relative != System.IO.Path.GetFileName(Path))
            throw new InvalidOperationException("Unexpected test cleanup path.");
        Directory.Delete(Path, recursive: true);
    }
}
