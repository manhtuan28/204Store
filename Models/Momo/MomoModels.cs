namespace _204Store.Models.Momo
{
    public class MomoOptionModel
    {
        public string MomoApiUrl { get; set; }
        public string SecretKey { get; set; }
        public string AccessKey { get; set; }
        public string PartnerCode { get; set; }
        public string RequestType { get; set; }
        public string ReturnUrl { get; set; }
        public string NotifyUrl { get; set; }
    }

    public class MomoCreatePaymentRequest
    {
        public string partnerCode { get; set; }
        public string requestId { get; set; }
        public long amount { get; set; }
        public string orderId { get; set; }
        public string orderInfo { get; set; }
        public string redirectUrl { get; set; }
        public string ipnUrl { get; set; }
        public string requestType { get; set; }
        public string extraData { get; set; }
        public string lang { get; set; } = "vi";
        public string signature { get; set; }
    }

    public class MomoCreatePaymentResponse
    {
        public string requestId { get; set; }
        public int resultCode { get; set; }
        public string payUrl { get; set; }
        public string message { get; set; }
    }
}