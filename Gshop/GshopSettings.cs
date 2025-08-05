namespace Ultimate_Gshop.Gshop
{
    public class GshopSettings
    {
        public string? LastGshopLoaded { get; set; }
        public bool AlwaysFetchLastGshop { get; set; }
        public bool AutoSave {  get; set; }
        public int? SaveFromSeconds { get; set; }
    }
}
