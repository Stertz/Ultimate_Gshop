using Ultimate_Gshop.Gshop;

namespace Ultimate_Gshop.Categories.Services;

internal static class EditCategories
{
    private static Category GetCategory(Shop shop, int categoryIndex)
    {
        if (categoryIndex < 0 || categoryIndex >= shop.Categories.Count)
            throw new ArgumentOutOfRangeException(nameof(categoryIndex));
        return shop.Categories[categoryIndex];
    }

    private static void ValidateSubCategory(Category category, int subCategoryIndex)
    {
        if (subCategoryIndex < 0 || subCategoryIndex >= category.SubCategories.Count)
            throw new ArgumentOutOfRangeException(nameof(subCategoryIndex));
    }

    internal static int Reset(Shop shop, int categoryIndex, int? subCategoryIndex, ShopTracking tracking)
    {
        var category = GetCategory(shop, categoryIndex);
        if (subCategoryIndex is int sub) ValidateSubCategory(category, sub);
        bool InScope(Itens.Item item) => item.CategoryIndex == categoryIndex &&
            (subCategoryIndex is null || item.SubCategoryIndex == subCategoryIndex);
        var count = shop.Itens.Count(InScope);
        if (count == 0) return 0;
        // A complete item snapshot also preserves prior index-based history and legacy Place values.
        tracking.ItemsReset(shop.Itens);
        shop.Itens.RemoveAll(item => InScope(item));
        shop.ItemAmount = shop.Itens.Count;
        return count;
    }

    internal static bool RenameCategory(Shop shop, int categoryIndex, string? title, ShopTracking tracking)
    {
        var category = GetCategory(shop, categoryIndex);
        if (string.IsNullOrWhiteSpace(title) || category.Title == title) return false;
        tracking.CategoryUpdated(categoryIndex, category);
        category.Title = title;
        return true;
    }

    internal static bool AddSubCategory(Shop shop, int categoryIndex, string? title, ShopTracking tracking)
    {
        var category = GetCategory(shop, categoryIndex);
        if (string.IsNullOrWhiteSpace(title) || category.SubCategories.Count >= 11) return false;
        tracking.CategoryUpdated(categoryIndex, category);
        category.SubCategories.Add(title);
        return true;
    }

    internal static bool RenameSubCategory(Shop shop, int categoryIndex, int subCategoryIndex, string? title, ShopTracking tracking)
    {
        var category = GetCategory(shop, categoryIndex);
        ValidateSubCategory(category, subCategoryIndex);
        var name = title?.Trim();
        if (string.IsNullOrEmpty(name) || category.SubCategories[subCategoryIndex] == name ||
            category.SubCategories.Where((_, index) => index != subCategoryIndex).Contains(name, StringComparer.OrdinalIgnoreCase))
            return false;
        tracking.CategoryUpdated(categoryIndex, category);
        category.SubCategories[subCategoryIndex] = name;
        return true;
    }

    internal static bool DeleteSubCategory(Shop shop, int categoryIndex, int subCategoryIndex, ShopTracking tracking)
    {
        var category = GetCategory(shop, categoryIndex);
        ValidateSubCategory(category, subCategoryIndex);
        if (shop.Itens.Any(item => item.CategoryIndex == categoryIndex && item.SubCategoryIndex == subCategoryIndex))
            return false;
        var affected = shop.Itens.Select((item, index) => (Item: item, Index: index))
            .Where(entry => entry.Item.CategoryIndex == categoryIndex && entry.Item.SubCategoryIndex > subCategoryIndex)
            .ToDictionary(entry => entry.Index, entry => entry.Item);
        tracking.CategoryUpdated(categoryIndex, category, affected);
        category.SubCategories.RemoveAt(subCategoryIndex);
        foreach (var item in affected.Values) item.SubCategoryIndex--;
        return true;
    }
}
