using Ultimate_Gshop.Itens;
namespace Ultimate_Gshop.Gshop
{
    public class ShopChange
    {
        public int Index { get; set; }
        public Item? ItemBackup { get; set; }
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<int, Item>? ItemsBackup { get; set; }
        public EChangeAction Action { get; set; }
    }
    public enum EChangeAction
    {
        Insert,
        Update,
        Delete,
        Reorder
    }
}
