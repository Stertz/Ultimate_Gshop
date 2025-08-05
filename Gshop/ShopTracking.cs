using Ultimate_Gshop.Itens;
namespace Ultimate_Gshop.Gshop
{
    public class ShopTracking
    {
        public Stack<ShopChange> UndoChanges { get; } = [];
        public Queue<ShopChange> RedoChanges { get; } = [];
        public void ItemDeleted(int index, Item item)
        {
            var op = new ShopChange();
            op.Index = index;
            op.Action = EChangeAction.Delete;
            op.ItemBackup = item;
            UndoChanges.Push(op);
        }
        public void ItemUpdated(int index, Item old_item)
        {
            var op = new ShopChange();
            op.Index = index;
            op.Action = EChangeAction.Update;
            op.ItemBackup = old_item;
            UndoChanges.Push(op);
        }
        public void ItemCreated(int index)
        {
            var op = new ShopChange();
            op.Index = index;
            op.Action = EChangeAction.Insert;
            UndoChanges.Push(op);
        }

        public void Undo(Shop shop)
        {
            var op = UndoChanges.Pop();

            switch (op.Action)
            {
                case EChangeAction.Delete:
                    if (op.ItemBackup is null) throw new InvalidOperationException("Can't undo null reference");
                    shop.Itens[op.Index] = op.ItemBackup;
                    break;
                case EChangeAction.Update:
                    if (op.ItemBackup is null) throw new InvalidOperationException("Can't undo null reference");
                    shop.Itens[op.Index] = op.ItemBackup;
                    break;
                default:
                    var item = shop.Itens[op.Index];
                    RedoChanges.Enqueue(new ShopChange() { Index = op.Index, Action = EChangeAction.Delete, ItemBackup = item });
                    shop.Itens.RemoveAt(op.Index);
                    break;
            }
        }
    }
}
