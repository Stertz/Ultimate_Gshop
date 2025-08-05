using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultimate_Gshop.Itens.Services
{
    public static class InsertItem
    {
        public static void Handle(List<Item> itens, Item item)
        {
            if (itens.Exists(i => i.ID == item.ID)) throw new InvalidOperationException("Item ID already exists");
            itens.Add(item);
        }

        public static void Validate(Item item)
        {

        }


    }
}
