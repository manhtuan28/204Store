namespace _204Store.Models.ViewModels
{
    public class HomeViewModel
    {
        public IEnumerable<Category> Categories { get; set; }
        public IEnumerable<Product> FeaturedProducts { get; set; }
        public IEnumerable<Product> BestSellerProducts { get; set; }
        public IEnumerable<Product> SaleProducts { get; set; }
        public IEnumerable<Product> AppleProducts { get; set; }
        public IEnumerable<Product> SamsungProducts { get; set; }
    }
}