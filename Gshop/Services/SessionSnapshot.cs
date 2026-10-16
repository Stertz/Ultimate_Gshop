using Ultimate_Gshop.Categories;

namespace Ultimate_Gshop.Gshop.Services;

// Capture on the session's owning thread. Only this object can access the detached data.
internal sealed class SessionSnapshot
{
    private readonly Shop _shop;
    private readonly ShopTracking _tracking;
    private readonly GshopSettings _settings;

    private SessionSnapshot(Shop shop, ShopTracking tracking, GshopSettings settings)
        => (_shop, _tracking, _settings) = (shop, tracking, settings);

    internal static SessionSnapshot Capture(Shop shop, ShopTracking tracking, GshopSettings settings) => new(
        new Shop
        {
            Timestamp = shop.Timestamp,
            ItemAmount = shop.Itens.Count,
            Categories = shop.Categories.Select(CloneCategory).ToList(),
            Itens = shop.Itens.Select(item => item.Clone()).ToList()
        },
        new ShopTracking
        {
            UndoChanges = CloneStack(tracking.UndoChanges),
            RedoChanges = CloneStack(tracking.RedoChanges)
        },
        new GshopSettings
        {
            AutoSave = settings.AutoSave,
            SaveFromSeconds = settings.SaveFromSeconds,
            LastGshopLoadedFrom = settings.LastGshopLoadedFrom,
            LastBackupFrom = settings.LastBackupFrom
        });

    private static Category CloneCategory(Category category) => new()
    {
        Title = category.Title,
        SubCategories = [.. category.SubCategories]
    };

    private static Stack<ShopChange> CloneStack(Stack<ShopChange> changes) => new(changes.Reverse().Select(change => new ShopChange
    {
        Index = change.Index,
        Action = change.Action,
        ItemBackup = change.ItemBackup?.Clone(),
        ItemsBackup = change.ItemsBackup?.ToDictionary(entry => entry.Key, entry => entry.Value.Clone()),
        ItemListBackup = change.ItemListBackup?.Select(item => item.Clone()).ToList(),
        CategoryBackup = change.CategoryBackup is null ? null : CloneCategory(change.CategoryBackup)
    }));

    internal void WriteTo(string directory) => SaveGshop.HandleSession(_shop, _tracking, _settings, directory);

    internal Task WriteAsync(string directory) => Task.Run(() => WriteTo(directory));
}
