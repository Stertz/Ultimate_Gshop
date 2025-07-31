namespace Ultimate_Gshop.Categories
{
    public class Category
    {
        public required string Title { get; set; }
        public List<string> SubCategories { get; set; } = [];
    }
}