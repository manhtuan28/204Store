namespace _204Store.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Product> FeaturedProducts { get; set; } = new List<Product>();
        public List<Product> BestSellerProducts { get; set; } = new List<Product>();
        public List<Product> SaleProducts { get; set; } = new List<Product>();

        public List<Product> AppleProducts { get; set; } = new List<Product>();
        public List<Product> SamsungProducts { get; set; } = new List<Product>();
        public List<Product> LaptopProducts { get; set; } = new List<Product>();
        public List<Product> AccessoryProducts { get; set; } = new List<Product>();
    }
}