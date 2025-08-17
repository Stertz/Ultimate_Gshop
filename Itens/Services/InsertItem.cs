using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ultimate_Gshop.Gshop;

namespace Ultimate_Gshop.Itens.Services
{
    public static class InsertItem
    {
        public static void Handle(List<Item> itens, Item item, ShopTracking tracking)
        {
            //if (itens.Exists(i => i.ID == item.ID)) throw new InvalidOperationException("Item ID already exists");
            itens.Add(item);
            tracking.ItemCreated(itens.Count - 1, item);
        }

        public static void Validate(Item item)
        {

        }


    }
}
