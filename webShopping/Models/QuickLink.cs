namespace webShopping.Models
{
    public class QuickLink
    {
        public int Id { get; set; }
        public string TitleEn { get; set; } = "";
        public string TitleAr { get; set; } = "";
        public string Url { get; set; } = "/";
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
