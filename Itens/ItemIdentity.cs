namespace Ultimate_Gshop.Itens;

internal static class ItemIdentity
{
    internal static int FindIndex(List<Item> items, Item original)
    {
        var index = items.FindIndex(item => ReferenceEquals(item, original));
        if (index >= 0) return index;

        // Detached snapshots must identify one record; game IDs alone are not unique.
        for (var candidate = 0; candidate < items.Count; candidate++)
        {
            if (items[candidate].ID != original.ID || items[candidate].Place != original.Place) continue;
            if (index >= 0)
                throw new InvalidOperationException("The original item identifies more than one shop record.");
            index = candidate;
        }
        return index;
    }
}
