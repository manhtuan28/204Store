using _204Store.Models;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace _204Store.Services
{
    public class VnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreatePaymentUrl(OrderHeader model, HttpContext context)
        {
            var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);
            var tick = DateTime.Now.Ticks;
            var pay = new VnPayLibrary();
            var urlCallBack = _configuration["VnPay:ReturnUrl"];

            pay.AddRequestData("vnp_Version", "2.1.0");
            pay.AddRequestData("vnp_Command", "pay");
            pay.AddRequestData("vnp_TmnCode", _configuration["VnPay:TmnCode"]);
            pay.AddRequestData("vnp_Amount", ((long)model.OrderTotal * 100).ToString());
            pay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", "VND");
            pay.AddRequestData("vnp_IpAddr", pay.GetIpAddress(context));
            pay.AddRequestData("vnp_Locale", "vn");
            pay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {model.Id}");
            pay.AddRequestData("vnp_OrderType", "other");
            pay.AddRequestData("vnp_ReturnUrl", urlCallBack);
            pay.AddRequestData("vnp_TxnRef", $"{model.Id}_{tick}");

            var paymentUrl = pay.CreateRequestUrl(_configuration["VnPay:BaseUrl"], _configuration["VnPay:HashSecret"]);
            return paymentUrl;
        }

        public VnPayResponseModel PaymentExecute(IQueryCollection collections)
        {
            var pay = new VnPayLibrary();
            var response = pay.GetFullResponseData(collections, _configuration["VnPay:HashSecret"]);
            return response;
        }
    }
    public class VnPayLibrary
    {
        private SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
        private SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());

        public void AddRequestData(string key, string value) { if (!string.IsNullOrEmpty(value)) _requestData.Add(key, value); }
        public void AddResponseData(string key, string value) { if (!string.IsNullOrEmpty(value)) _responseData.Add(key, value); }

        public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
        {
            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in _requestData)
            {
                if (data.Length > 0) data.Append('&');
                data.Append(kv.Key + "=" + WebUtility.UrlEncode(kv.Value));
            }
            string queryString = data.ToString();
            string vnp_SecureHash = Utils.HmacSHA512(vnp_HashSecret, queryString);
            return baseUrl + "?" + queryString + "&vnp_SecureHash=" + vnp_SecureHash;
        }

        public VnPayResponseModel GetFullResponseData(IQueryCollection collection, string hashSecret)
        {
            var vnPay = new VnPayLibrary();
            foreach (var (key, value) in collection)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnPay.AddResponseData(key, value.ToString());
                }
            }

            long orderId = Convert.ToInt64(vnPay.GetResponseData("vnp_TxnRef").Split('_')[0]);
            long vnpayTranId = Convert.ToInt64(vnPay.GetResponseData("vnp_TransactionNo"));
            string vnp_ResponseCode = vnPay.GetResponseData("vnp_ResponseCode");
            string vnp_SecureHash = collection.FirstOrDefault(p => p.Key == "vnp_SecureHash").Value;
            string terminalID = collection.FirstOrDefault(p => p.Key == "vnp_TmnCode").Value;
            long vnp_Amount = Convert.ToInt64(vnPay.GetResponseData("vnp_Amount")) / 100;
            String bankCode = vnPay.GetResponseData("vnp_BankCode");

            bool checkSignature = vnPay.ValidateSignature(vnp_SecureHash, hashSecret);
            if (!checkSignature) return new VnPayResponseModel { Success = false };

            return new VnPayResponseModel
            {
                Success = true,
                PaymentMethod = "VnPay",
                OrderDescription = $"Đơn hàng: {orderId}",
                OrderId = orderId.ToString(),
                PaymentId = vnpayTranId.ToString(),
                TransactionId = vnpayTranId.ToString(),
                Token = vnp_SecureHash,
                VnPayResponseCode = vnp_ResponseCode
            };
        }
        public string GetIpAddress(HttpContext context)
        {
            var ipAddress = string.Empty;
            try { ipAddress = context.Connection.RemoteIpAddress?.ToString(); } catch { }
            return string.IsNullOrEmpty(ipAddress) ? "127.0.0.1" : ipAddress;
        }
        public bool ValidateSignature(string inputHash, string secretKey)
        {
            string rspRaw = GetResponseData();
            string myChecksum = Utils.HmacSHA512(secretKey, rspRaw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }
        private string GetResponseData()
        {
            StringBuilder data = new StringBuilder();
            if (_responseData.ContainsKey("vnp_SecureHashType")) _responseData.Remove("vnp_SecureHashType");
            if (_responseData.ContainsKey("vnp_SecureHash")) _responseData.Remove("vnp_SecureHash");
            foreach (KeyValuePair<string, string> kv in _responseData)
            {
                if (data.Length > 0) data.Append('&');
                data.Append(kv.Key + "=" + WebUtility.UrlEncode(kv.Value));
            }
            return data.ToString();
        }
        public string GetResponseData(string key) => _responseData.TryGetValue(key, out string retValue) ? retValue : string.Empty;
    }
    public class VnPayCompare : IComparer<string> { public int Compare(string x, string y) { if (x == y) return 0; if (x == null) return -1; if (y == null) return 1; var vnpCompare = CompareInfo.GetCompareInfo("en-US"); return vnpCompare.Compare(x, y, CompareOptions.Ordinal); } }
    public class Utils { public static string HmacSHA512(string key, string inputData) { var hash = new StringBuilder(); byte[] keyBytes = Encoding.UTF8.GetBytes(key); byte[] inputBytes = Encoding.UTF8.GetBytes(inputData); using (var hmac = new HMACSHA512(keyBytes)) { byte[] hashValue = hmac.ComputeHash(inputBytes); foreach (var theByte in hashValue) hash.Append(theByte.ToString("x2")); } return hash.ToString(); } }
    public class VnPayResponseModel { public bool Success { get; set; } public string PaymentMethod { get; set; } public string OrderDescription { get; set; } public string OrderId { get; set; } public string PaymentId { get; set; } public string TransactionId { get; set; } public string Token { get; set; } public string VnPayResponseCode { get; set; } }
}