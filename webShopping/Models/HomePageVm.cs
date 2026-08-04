namespace webShopping.Models
{
    public class HomePageVm
    {
        public List<Product> AllProducts { get; set; } = new();
        public List<Product> Featured { get; set; } = new();
        public List<Product> Newest { get; set; } = new();
        public List<Product> BestSellers { get; set; } = new();
        public List<Product> Offers { get; set; } = new();
        public List<Categoty> Categories { get; set; } = new();
        public List<Brand> Brands { get; set; } = new();
        public List<Banner> Banners { get; set; } = new();
        public List<ProductReview> Reviews { get; set; } = new();
    }
}
