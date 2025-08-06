using Avalonia.Controls;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Views;

namespace Ultimate_Gshop.Itens.Services
{
    public static class DeleteItem
    {
        public static async Task<bool> Handle(Item item, List<Item> itens, ShopTracking tracking, Window window)
        {

            var msgBox = new CustomMessageBox(
            "Exclusão de Item",
            $"Deseja realmente excluir o item \"{item.Name}\" (ID: {item.ID})?\n\n",
            CustomMessageBox.MessageBoxIcon.Question,
            "Sim", "Não");


            var result = await msgBox.ShowDialogAsync(window);

            if (result == "Sim")
            {
                var index = itens.IndexOf(item);
                if (itens.Remove(item))
                {
                    tracking.ItemDeleted(index, item);
                    return true;
                }
            }

            return false;

        }
    }
}
