namespace Ultimate_Gshop.Itens;

internal sealed class ItemSelection
{
    internal List<Item> Items { get; } = [];
    private Item? _anchor;

    internal void Reset()
    {
        Items.Clear();
        _anchor = null;
    }

    internal void Select(IReadOnlyList<Item> visible, Item clicked, bool extend, bool toggle)
    {
        var indices = new Dictionary<Item, int>(ReferenceEqualityComparer.Instance);
        for (var index = 0; index < visible.Count; index++) indices.TryAdd(visible[index], index);
        if (!indices.TryGetValue(clicked, out var target)) return;
        // A refreshed/filtered source cannot retain hidden or stale item references.
        Items.RemoveAll(item => !indices.ContainsKey(item));
        var anchor = _anchor is not null && indices.TryGetValue(_anchor, out var anchorIndex) ? anchorIndex : -1;
        if (extend && anchor >= 0)
        {
            if (!toggle) Items.Clear();
            var selectedItems = new HashSet<Item>(Items, ReferenceEqualityComparer.Instance);
            for (var index = Math.Min(anchor, target); index <= Math.Max(anchor, target); index++)
                if (selectedItems.Add(visible[index])) Items.Add(visible[index]);
            return;
        }

        if (!toggle) Items.Clear();
        var selected = FindReference(Items, clicked);
        if (toggle && selected >= 0) Items.RemoveAt(selected);
        else Items.Add(clicked);
        _anchor = clicked;
    }

    private static int FindReference(IReadOnlyList<Item> items, Item target)
    {
        for (var index = 0; index < items.Count; index++)
            if (ReferenceEquals(items[index], target)) return index;
        return -1;
    }
}
