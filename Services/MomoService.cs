using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using _204Store.Models;
using _204Store.Models.Momo;

namespace _204Store.Services
{
    public class MomoService
    {
        private readonly IOptions<MomoOptionModel> _options;
        private readonly HttpClient _httpClient;

        public MomoService(IOptions<MomoOptionModel> options, HttpClient httpClient)
        {
            _options = options;
            _httpClient = httpClient;
        }

        public async Task<MomoCreatePaymentResponse> CreatePaymentAsync(OrderHeader model)
        {
            model.OrderDate = DateTime.Now;

            var orderId = model.Id.ToString() + "_" + DateTime.Now.Ticks;

            string khachHang = ToUnsignString(model.Name ?? "Khach");
            string sdt = model.PhoneNumber ?? "";

            var orderInfo = $"Don {model.Id} - {khachHang} - {sdt}";

            var amount = ((long)model.OrderTotal).ToString();
            var requestId = Guid.NewGuid().ToString();

            var rawData =
                $"accessKey={_options.Value.AccessKey}" +
                $"&amount={amount}" +
                $"&extraData=" +
                $"&ipnUrl={_options.Value.NotifyUrl}" +
                $"&orderId={orderId}" +
                $"&orderInfo={orderInfo}" +
                $"&partnerCode={_options.Value.PartnerCode}" +
                $"&redirectUrl={_options.Value.ReturnUrl}" +
                $"&requestId={requestId}" +
                $"&requestType={_options.Value.RequestType}";

            var signature = ComputeHmacSha256(rawData, _options.Value.SecretKey);

            var request = new MomoCreatePaymentRequest
            {
                partnerCode = _options.Value.PartnerCode,
                requestId = requestId,
                amount = (long)model.OrderTotal,
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = _options.Value.ReturnUrl,
                ipnUrl = _options.Value.NotifyUrl,
                requestType = _options.Value.RequestType,
                extraData = "",
                lang = "vi",
                signature = signature
            };

            var requestContent = JsonConvert.SerializeObject(request);
            var content = new StringContent(requestContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_options.Value.MomoApiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            try
            {
                var momoResponse = JsonConvert.DeserializeObject<MomoCreatePaymentResponse>(responseContent);
                if (momoResponse.resultCode != 0)
                {
                    momoResponse.message = momoResponse.message ?? ("Lỗi: " + responseContent);
                }
                return momoResponse;
            }
            catch
            {
                return new MomoCreatePaymentResponse
                {
                    resultCode = -1,
                    message = $"Lỗi không xác định: {responseContent}"
                };
            }
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            byte[] hashBytes;

            using (var hmac = new HMACSHA256(keyBytes))
            {
                hashBytes = hmac.ComputeHash(messageBytes);
            }

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        private string ToUnsignString(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            input = input.Trim();
            for (int i = 0x20; i < 0x30; i++)
            {
                input = input.Replace(((char)i).ToString(), " ");
            }
            input = input.Replace(".", "-");
            input = input.Replace(" ", "-");

            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = input.Normalize(NormalizationForm.FormD);
            string result = regex.Replace(temp, string.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D');

            return result;
        }
    }
}