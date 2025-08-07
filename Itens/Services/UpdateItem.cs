using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ultimate_Gshop.Gshop;

namespace Ultimate_Gshop.Itens.Services
{
    public static class UpdateItem
    {
        public static Item Handle(List<Item> itens, Item item, Item old_item, ShopTracking tracking)
        {
            var index = itens.FindIndex(i => i.ID == item.ID);

            if (index == -1) throw new InvalidOperationException("Couldn't find the item being edited on the Shop");

            if (ItemComparer.Handle(item, old_item)) return old_item;

            itens[index] = item;

            tracking.ItemUpdated(index, old_item);

            return item;

        }
    }
}
