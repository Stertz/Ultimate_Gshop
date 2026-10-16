using System.Globalization;

namespace Ultimate_Gshop.Itens.Services;

internal static class FilterItems
{
    internal static List<Item> Handle(IEnumerable<Item> items, int category, int subCategory, string? text)
    {
        var query = text?.Trim() ?? string.Empty;
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var searchId = query.Length > 0 && query.All(char.IsDigit);

        // Always start from the shop, so broadening a query restores matching records.
        return items.Where(item =>
            (category < 0 || item.CategoryIndex == category) &&
            (category < 0 || subCategory < 0 || item.SubCategoryIndex == subCategory) &&
            (terms.All(term => (item.Name ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase)) ||
             (searchId && item.ID.ToString(CultureInfo.InvariantCulture).Contains(query))))
            .ToList();
    }
}
