using System.ComponentModel;
using System.Text.Json;
using Ultimate_Gshop.Itens;
using Ultimate_Gshop.Misc;
namespace Ultimate_Gshop.Gshop
{
    public class ShopTracking : INotifyPropertyChanged
    {
        public Stack<ShopChange> UndoChanges { get; } = new();
        public Stack<ShopChange> RedoChanges { get; } = new();

        public bool CanUndo => UndoChanges.Count > 0;
        public bool CanRedo => RedoChanges.Count > 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void ItemDeleted(int index, Item item)
        {
            UndoChanges.Push(new ShopChange
            {
                Index = index,
                Action = EChangeAction.Delete,
                ItemBackup = item.Clone()
            });
            RedoChanges.Clear();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanUndo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanRedo)));
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanUndo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanRedo)));
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanUndo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanRedo)));
        }

        public void Undo(Shop shop)
        {
            if (UndoChanges.Count == 0)
                return;

            var op = UndoChanges.Pop();

            switch (op.Action)
            {
                case EChangeAction.Delete:
                    shop.Itens.Insert(op.Index, op.ItemBackup!.Clone());
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
                    shop.Itens[op.Index] = op.ItemBackup!.Clone();
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

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanUndo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanRedo)));

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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanUndo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanRedo)));
        }

        private static readonly string _tracking = Path.Combine(ApplicationSettings.AppFolder, "tracking.json");

        public static ShopTracking Load()
        {
            if (!File.Exists(_tracking))
                return new ShopTracking();

            var json = File.ReadAllText(_tracking);
            return JsonSerializer.Deserialize<ShopTracking>(json) ?? new ShopTracking();
        }

        public static void Save(ShopTracking tracking)
        {
            Directory.CreateDirectory(ApplicationSettings.AppFolder);
            var json = JsonSerializer.Serialize(tracking, ApplicationSettings.IdentedJson);
            File.WriteAllText(_tracking, json);
        }

    }
}