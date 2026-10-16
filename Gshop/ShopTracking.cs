using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ultimate_Gshop.Gshop.Misc;
using Ultimate_Gshop.Itens;
using Ultimate_Gshop.Misc;
namespace Ultimate_Gshop.Gshop
{
    public class ShopTracking : INotifyPropertyChanged
    {
        [JsonConverter(typeof(ShopChangeStackConverter))]
        public Stack<ShopChange> UndoChanges { get; set; } = new();
        [JsonConverter(typeof(ShopChangeStackConverter))]
        public Stack<ShopChange> RedoChanges { get; set; } = new();

        public bool CanUndo => UndoChanges.Count > 0;
        public bool CanRedo => RedoChanges.Count > 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Clear()
        {
            UndoChanges.Clear();
            RedoChanges.Clear();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanUndo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanRedo)));
        }

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
                ItemBackup = oldItem!.Clone()
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

        internal void ItemsReordered(Dictionary<int, Item> originalItems)
        {
            if (originalItems.Count == 0) return;
            UndoChanges.Push(new ShopChange
            {
                Action = EChangeAction.Reorder,
                ItemsBackup = originalItems.ToDictionary(entry => entry.Key, entry => entry.Value.Clone())
            });
            RedoChanges.Clear();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanUndo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanRedo)));
        }

        internal void ItemsReset(List<Item> items)
        {
            Record(new ShopChange { Action = EChangeAction.ResetItems, ItemListBackup = items.Select(item => item.Clone()).ToList() });
        }

        internal void CategoryUpdated(int index, Categories.Category category, Dictionary<int, Item>? affectedItems = null)
        {
            Record(new ShopChange
            {
                Action = EChangeAction.Category, Index = index,
                CategoryBackup = CloneCategory(category),
                ItemsBackup = affectedItems?.ToDictionary(entry => entry.Key, entry => entry.Value.Clone())
            });
        }

        private void Record(ShopChange change)
        {
            UndoChanges.Push(change);
            RedoChanges.Clear();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanUndo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanRedo)));
        }

        private static Categories.Category CloneCategory(Categories.Category category) =>
            new() { Title = category.Title, SubCategories = [.. category.SubCategories] };

        private static ShopChange ApplyReset(Shop shop, ShopChange change)
        {
            if (change.ItemListBackup is null || change.ItemListBackup.Any(item => item is null))
                throw new InvalidDataException("The reset history contains an invalid item list.");
            var restored = change.ItemListBackup.Select(item => item.Clone()).ToList();
            var inverse = new ShopChange
            {
                Action = EChangeAction.ResetItems,
                ItemListBackup = shop.Itens.Select(item => item.Clone()).ToList()
            };
            shop.Itens.Clear();
            shop.Itens.AddRange(restored);
            return inverse;
        }

        private static ShopChange ApplyCategory(Shop shop, ShopChange change)
        {
            if (change.Index < 0 || change.Index >= shop.Categories.Count || change.CategoryBackup is null ||
                change.ItemsBackup?.Any(entry => entry.Key < 0 || entry.Key >= shop.Itens.Count || entry.Value is null) == true)
                throw new InvalidDataException("The category history contains invalid indices or missing metadata.");
            var category = CloneCategory(change.CategoryBackup);
            var restoredItems = change.ItemsBackup?.ToDictionary(entry => entry.Key, entry => entry.Value.Clone());
            var inverse = new ShopChange
            {
                Action = EChangeAction.Category, Index = change.Index,
                CategoryBackup = CloneCategory(shop.Categories[change.Index]),
                ItemsBackup = restoredItems?.Keys.ToDictionary(index => index, index => shop.Itens[index].Clone())
            };
            shop.Categories[change.Index] = category;
            if (restoredItems is not null)
                foreach (var (index, item) in restoredItems) shop.Itens[index] = item;
            return inverse;
        }

        private static ShopChange ApplyReorder(Shop shop, ShopChange change)
        {
            var items = change.ItemsBackup;
            if (items is null || items.Count == 0 ||
                items.Any(entry => entry.Key < 0 || entry.Key >= shop.Itens.Count || entry.Value is null))
                throw new InvalidDataException("The reorder history contains invalid item indices.");

            var inverse = new ShopChange
            {
                Action = EChangeAction.Reorder,
                ItemsBackup = items.Keys.ToDictionary(index => index, index => shop.Itens[index].Clone())
            };
            foreach (var (index, item) in items)
                shop.Itens[index] = item.Clone();
            return inverse;
        }

        public void Undo(Shop shop)
        {
            if (UndoChanges.Count == 0)
                return;

            var op = UndoChanges.Peek();

            switch (op.Action)
            {
                case EChangeAction.ResetItems:
                    RedoChanges.Push(ApplyReset(shop, op));
                    break;
                case EChangeAction.Category:
                    RedoChanges.Push(ApplyCategory(shop, op));
                    break;
                case EChangeAction.Reorder:
                    RedoChanges.Push(ApplyReorder(shop, op));
                    break;
                case EChangeAction.Delete:
                    shop.Itens.Insert(op.Index, op.ItemBackup!.Clone());
                    RedoChanges.Push(new ShopChange
                    {
                        Index = op.Index,
                        Action = EChangeAction.Delete
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
                        Action = EChangeAction.Insert,
                        ItemBackup = removed.Clone()
                    });
                    shop.Itens.RemoveAt(op.Index);
                    break;
            }

            UndoChanges.Pop();
            shop.ItemAmount = shop.Itens.Count;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanUndo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanRedo)));

        }

        public void Redo(Shop shop)
        {
            if (RedoChanges.Count == 0)
                return;

            var op = RedoChanges.Peek();

            switch (op.Action)
            {
                case EChangeAction.ResetItems:
                    UndoChanges.Push(ApplyReset(shop, op));
                    break;
                case EChangeAction.Category:
                    UndoChanges.Push(ApplyCategory(shop, op));
                    break;
                case EChangeAction.Reorder:
                    UndoChanges.Push(ApplyReorder(shop, op));
                    break;
                case EChangeAction.Delete:
                    var removed = shop.Itens[op.Index];
                    UndoChanges.Push(new ShopChange
                    {
                        Index = op.Index,
                        Action = EChangeAction.Delete,
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
                        Action = EChangeAction.Insert,
                        ItemBackup = op.ItemBackup.Clone()
                    });
                    break;
            }
            RedoChanges.Pop();
            shop.ItemAmount = shop.Itens.Count;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanUndo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanRedo)));
        }

        private static readonly string _tracking = Path.Combine(ApplicationSettings.AppFolder, "tracking.json");

        public static ShopTracking Load() => Load(_tracking);

        public static ShopTracking Load(string path) => SessionTransaction.Read(Path.GetDirectoryName(Path.GetFullPath(path))!, () =>
            File.Exists(path) ? JsonSerializer.Deserialize<ShopTracking>(File.ReadAllText(path)) ?? new ShopTracking()
                : new ShopTracking());

        public static void Save(ShopTracking tracking)
        {
            Directory.CreateDirectory(ApplicationSettings.AppFolder);
            Save(tracking, _tracking);
        }

        public static void Save(ShopTracking tracking, string path)
        {
            SessionTransaction.Read(Path.GetDirectoryName(Path.GetFullPath(path))!, () =>
            {
                ProtectedFile.Write(path, stream => JsonSerializer.Serialize(stream, tracking, ApplicationSettings.IdentedJson));
                return true;
            });
        }


    }
}
