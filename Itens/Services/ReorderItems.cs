using Ultimate_Gshop.Gshop;

namespace Ultimate_Gshop.Itens.Services;

internal static class ReorderItems
{
    // TargetIndex is an insertion boundary in the visible list before removing the source.
    internal static Item? Handle(List<Item> items, IEnumerable<Item> visibleItems,
        int sourcePlace, int targetIndex, ShopTracking tracking)
    {
        var visible = new HashSet<Item>(visibleItems, ReferenceEqualityComparer.Instance);
        var scope = items.Select((item, index) => (Item: item, Index: index))
            .Where(entry => visible.Contains(entry.Item)).OrderBy(entry => entry.Item.Place).ToList();
        var sourceIndex = scope.FindIndex(entry => entry.Item.Place == sourcePlace);
        if (sourceIndex < 0) return null;
        if (scope.Select(entry => entry.Item.Place).Distinct().Count() != scope.Count)
            throw new InvalidOperationException("Reordering requires unique visible item positions.");

        var slots = scope.Select(entry => entry.Item.Place).ToArray();
        var indices = scope.Select(entry => entry.Index).ToArray();
        var moving = scope[sourceIndex];
        var insertionIndex = Math.Clamp(targetIndex, 0, scope.Count);
        if (insertionIndex > sourceIndex) insertionIndex--;
        if (insertionIndex == sourceIndex) return moving.Item;

        scope.RemoveAt(sourceIndex);
        scope.Insert(insertionIndex, moving);
        var changedIndices = indices.Where((index, position) => !ReferenceEquals(items[index], scope[position].Item));
        tracking.ItemsReordered(changedIndices.ToDictionary(index => index, index => items[index]));
        // Reuse existing slots: filtered-out items retain their positions and cannot collide.
        for (var index = 0; index < scope.Count; index++)
        {
            scope[index].Item.Place = slots[index];
            // Persist the display order too: the legacy loader derives Place from record order.
            items[indices[index]] = scope[index].Item;
        }
        return moving.Item;
    }
}
