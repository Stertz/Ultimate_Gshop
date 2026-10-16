using Ultimate_Gshop.Gshop;
namespace Ultimate_Gshop.Itens.Services
{
    public static class UpdateItem
    {
        public static Item Handle(List<Item> itens, Item item, Item old_item, ShopTracking tracking)
        {
            var index = ItemIdentity.FindIndex(itens, old_item);

            if (index == -1) throw new InvalidOperationException("Couldn't find the item being edited on the Shop");

            var current = itens[index];
            if (ItemComparer.Handle(item, current)) return current;

            tracking.ItemUpdated(index, current);
            itens[index] = item;

            return item;

        }
    }
}
