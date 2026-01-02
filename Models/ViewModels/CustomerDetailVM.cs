namespace _204Store.Models.ViewModels
{
    public class CustomerDetailVM
    {
        public ApplicationUser UserInfo { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public int ReviewCount { get; set; }

        public List<OrderHeader> OrderHistory { get; set; }

        public List<OrderHeader> ReturnHistory { get; set; }

        public List<LoginHistory> LoginLogs { get; set; }
    }
}