using Ultimate_Gshop.Gshop;

namespace Ultimate_Gshop.Itens.Services;

internal static class UpdateItems
{
    internal static void Handle(List<Item> items, IEnumerable<Item> selectedItems,
        Action<Item> edit, ShopTracking tracking)
    {
        var selected = new HashSet<Item>(selectedItems, ReferenceEqualityComparer.Instance);
        // Stage independent clones before changing the shop or its history.
        var changes = items.Where(selected.Contains).Select(original =>
        {
            var updated = original.Clone();
            edit(updated);
            return (Original: original, Updated: updated);
        }).ToList();

        foreach (var change in changes)
            UpdateItem.Handle(items, change.Updated, change.Original, tracking);
    }
}
