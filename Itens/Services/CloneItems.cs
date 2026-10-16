using Ultimate_Gshop.Gshop;

namespace Ultimate_Gshop.Itens.Services;

internal static class CloneItems
{
    private static int NextPlace(IReadOnlyCollection<Item> items) =>
        checked(Math.Max(-1, items.Count == 0 ? -1 : items.Max(item => item.Place)) + 1);

    internal static Item Prepare(Item source, IReadOnlyCollection<Item> items)
    {
        var place = NextPlace(items);
        var clone = source.Clone();
        clone.Place = place;
        return clone;
    }

    internal static List<Item> Handle(Shop shop, IEnumerable<Item> selectedItems, ShopTracking tracking)
    {
        var live = new HashSet<Item>(shop.Itens, ReferenceEqualityComparer.Instance);
        var seen = new HashSet<Item>(ReferenceEqualityComparer.Instance);
        var sources = selectedItems.Where(item => live.Contains(item) && seen.Add(item)).ToList();
        if (sources.Count == 0) return [];
        var firstPlace = NextPlace(shop.Itens);
        // Reject exhausted position space before inserting any clone or changing history.
        checked { _ = firstPlace + (sources.Count - 1); }
        var clones = sources.Select((source, index) =>
        {
            var clone = source.Clone();
            clone.Place = firstPlace + index;
            return clone;
        }).ToList();
        foreach (var clone in clones) InsertItem.Handle(shop.Itens, clone, tracking);
        shop.ItemAmount = shop.Itens.Count;
        return clones;
    }
}
