using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;

namespace MealPrepService.BusinessLogicLayer.Services
{
    public class VnpayService : IVnpayService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<VnpayService> _logger;
        
        private string VnpayUrl => _configuration["VnPay:Url"] ?? string.Empty;
        private string VnpayTmnCode => _configuration["VnPay:TmnCode"] ?? string.Empty;
        private string VnpayHashSecret => _configuration["VnPay:HashSecret"] ?? string.Empty;
        private string VnpayReturnUrl => _configuration["VnPay:ReturnUrl"] ?? string.Empty;
        
        public VnpayService(IConfiguration configuration, ILogger<VnpayService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }
        
        public Task<VnpayPaymentUrlDto> CreatePaymentUrlAsync(Guid orderId, decimal amount, string orderInfo)
        {
            var vnpayData = new SortedDictionary<string, string>
            {
                {"vnp_Version", "2.1.0"},
                {"vnp_Command", "pay"},
                {"vnp_TmnCode", VnpayTmnCode},
                {"vnp_Amount", ((long)(amount * 100)).ToString()}, // VNPay expects amount in VND cents
                {"vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")},
                {"vnp_CurrCode", "VND"},
                {"vnp_IpAddr", "127.0.0.1"}, // Should be actual client IP
                {"vnp_Locale", "vn"},
                {"vnp_OrderInfo", orderInfo},
                {"vnp_OrderType", "other"},
                {"vnp_ReturnUrl", VnpayReturnUrl},
                {"vnp_TxnRef", orderId.ToString()}
            };
            
            // Create secure hash
            var hashData = string.Join("&", vnpayData.Select(kv => $"{kv.Key}={kv.Value}"));
            var secureHash = CreateSecureHash(hashData, VnpayHashSecret);
            vnpayData.Add("vnp_SecureHash", secureHash);
            
            // Build payment URL
            var queryString = string.Join("&", vnpayData.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            var paymentUrl = $"{VnpayUrl}?{queryString}";
            
            return Task.FromResult(new VnpayPaymentUrlDto
            {
                PaymentUrl = paymentUrl,
                TransactionId = orderId.ToString()
            });
        }
        
        public Task<VnpayCallbackResult> ProcessCallbackAsync(VnpayCallbackDto callbackDto)
        {
            try
            {
                // Validate callback
                if (!ValidateCallback(callbackDto))
                {
                    return Task.FromResult(new VnpayCallbackResult
                    {
                        IsSuccess = false,
                        Message = "Invalid callback signature"
                    });
                }
                
                // Parse order ID
                if (!Guid.TryParse(callbackDto.vnp_TxnRef, out var orderId))
                {
                    return Task.FromResult(new VnpayCallbackResult
                    {
                        IsSuccess = false,
                        Message = "Invalid order ID format"
                    });
                }
                
                return Task.FromResult(new VnpayCallbackResult
                {
                    IsSuccess = true,
                    OrderId = orderId,
                    TransactionId = callbackDto.vnp_TransactionNo,
                    ResponseCode = callbackDto.vnp_ResponseCode,
                    Message = GetResponseMessage(callbackDto.vnp_ResponseCode)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPAY callback");
                return Task.FromResult(new VnpayCallbackResult
                {
                    IsSuccess = false,
                    Message = "Error processing callback"
                });
            }
        }
        
        public bool ValidateCallback(VnpayCallbackDto callbackDto)
        {
            try
            {
                // Extract all parameters except secure hash
                var vnpayData = new SortedDictionary<string, string>();
                
                var properties = typeof(VnpayCallbackDto).GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.Name == "vnp_SecureHash" || prop.Name == "vnp_SecureHashType")
                        continue;
                        
                    var value = prop.GetValue(callbackDto)?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        vnpayData.Add(prop.Name, value);
                    }
                }
                
                // Create hash data
                var hashData = string.Join("&", vnpayData.Select(kv => $"{kv.Key}={kv.Value}"));
                var computedHash = CreateSecureHash(hashData, VnpayHashSecret);
                
                return computedHash.Equals(callbackDto.vnp_SecureHash, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating VNPAY callback");
                return false;
            }
        }
        
        private string CreateSecureHash(string data, string secretKey)
        {
            using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
        
        private string GetResponseMessage(string responseCode)
        {
            return responseCode switch
            {
                "00" => "Payment successful",
                "07" => "Transaction deducted successfully. Transaction is suspected of fraud (related to gray card/black card)",
                "09" => "Customer's card/account has not registered for InternetBanking service at the bank",
                "10" => "Customer entered incorrect card/account information more than 3 times",
                "11" => "Payment deadline has expired. Please retry the transaction",
                "12" => "Customer's card/account is locked",
                "13" => "Customer entered incorrect transaction authentication password (OTP)",
                "24" => "Customer canceled the transaction",
                "51" => "Customer's account has insufficient balance to make the transaction",
                "65" => "Customer's account has exceeded the daily transaction limit",
                _ => "Transaction failed"
            };
        }
    }
}