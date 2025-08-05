using Ultimate_Gshop.Itens;

namespace Ultimate_Gshop.Gshop
{
    public class ShopTracking
    {
        public Stack<ShopChange> UndoChanges { get; } = new();
        public Stack<ShopChange> RedoChanges { get; } = new();

        public void ItemDeleted(int index, Item item)
        {
            UndoChanges.Push(new ShopChange
            {
                Index = index,
                Action = EChangeAction.Delete,
                ItemBackup = item.Clone()
            });
            RedoChanges.Clear();
        }

        public void ItemUpdated(int index, Item oldItem)
        {
            UndoChanges.Push(new ShopChange
            {
                Index = index,
                Action = EChangeAction.Update,
                ItemBackup = oldItem.Clone()
            });
            RedoChanges.Clear();
        }

        public void ItemCreated(int index, Item newItem)
        {
            UndoChanges.Push(new ShopChange
            {
                Index = index,
                Action = EChangeAction.Insert,
                ItemBackup = newItem.Clone()
            });
            RedoChanges.Clear();
        }

        public void Undo(Shop shop)
        {
            if (UndoChanges.Count == 0)
                return;

            var op = UndoChanges.Pop();

            switch (op.Action)
            {
                case EChangeAction.Delete:
                    if (op.ItemBackup is null) throw new InvalidOperationException("Can't undo empty object");
                    shop.Itens.Insert(op.Index, op.ItemBackup.Clone());
                    RedoChanges.Push(new ShopChange
                    {
                        Index = op.Index,
                        Action = EChangeAction.Insert
                    });
                    break;

                case EChangeAction.Update:
                    var currentItem = shop.Itens[op.Index];
                    RedoChanges.Push(new ShopChange
                    {
                        Index = op.Index,
                        Action = EChangeAction.Update,
                        ItemBackup = currentItem.Clone()
                    });
                    shop.Itens[op.Index] = op.ItemBackup?.Clone() ?? new Item();
                    break;

                case EChangeAction.Insert:
                    var removed = shop.Itens[op.Index];
                    RedoChanges.Push(new ShopChange
                    {
                        Index = op.Index,
                        Action = EChangeAction.Delete,
                        ItemBackup = removed.Clone()
                    });
                    shop.Itens.RemoveAt(op.Index);
                    break;
            }
        }

        public void Redo(Shop shop)
        {
            if (RedoChanges.Count == 0)
                return;

            var op = RedoChanges.Pop();

            switch (op.Action)
            {
                case EChangeAction.Delete:
                    var removed = shop.Itens[op.Index];
                    UndoChanges.Push(new ShopChange
                    {
                        Index = op.Index,
                        Action = EChangeAction.Insert,
                        ItemBackup = removed.Clone()
                    });
                    shop.Itens.RemoveAt(op.Index);
                    break;

                case EChangeAction.Update:
                    var currentItem = shop.Itens[op.Index];
                    UndoChanges.Push(new ShopChange
                    {
                        Index = op.Index,
                        Action = EChangeAction.Update,
                        ItemBackup = currentItem.Clone()
                    });
                    shop.Itens[op.Index] = op.ItemBackup!.Clone();
                    break;

                case EChangeAction.Insert:
                    shop.Itens.Insert(op.Index, op.ItemBackup!.Clone());
                    UndoChanges.Push(new ShopChange
                    {
                        Index = op.Index,
                        Action = EChangeAction.Delete,
                        ItemBackup = op.ItemBackup.Clone()
                    });
                    break;
            }
        }
    }
}