using Ultimate_Gshop.Gshop;
namespace Ultimate_Gshop.Itens.Services
{
    public static class UpdateItem
    {
        public static Item Handle(List<Item> itens, Item item, Item old_item, ShopTracking tracking)
        {
            var index = itens.FindIndex(i => ReferenceEquals(i, old_item));
            if (index == -1)
            {
                // A detached snapshot must identify exactly one existing record.
                for (var candidate = 0; candidate < itens.Count; candidate++)
                {
                    if (itens[candidate].ID != old_item.ID || itens[candidate].Place != old_item.Place)
                        continue;
                    if (index != -1)
                        throw new InvalidOperationException("The original item identifies more than one shop record.");
                    index = candidate;
                }
            }

            if (index == -1) throw new InvalidOperationException("Couldn't find the item being edited on the Shop");

            var current = itens[index];
            if (ItemComparer.Handle(item, current)) return current;

            tracking.ItemUpdated(index, current);
            itens[index] = item;

            return item;

        }
    }
}
