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
        public static void Handle(List<Item> itens, Item item)
        {
            var currentItem = itens.Find(i => i.ID == item.ID) 
                ?? throw new InvalidOperationException("The item being edited was not found");
            currentItem = item;
        }
    }
}
