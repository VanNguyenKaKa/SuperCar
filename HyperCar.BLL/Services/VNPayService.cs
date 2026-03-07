using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using HyperCar.DAL.Enums;
using HyperCar.DAL.Repositories;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace HyperCar.BLL.Services
{
    public class VNPayService : IVNPayService
    {
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _unitOfWork;

        public VNPayService(IConfiguration config, IUnitOfWork unitOfWork)
        {
            _config = config;
            _unitOfWork = unitOfWork;
        }
        public string CreatePaymentUrl(int orderId, decimal amount, string orderInfo, string ipAddress)
        {
            var vnpUrl = _config["VNPay:Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            var vnpTmnCode = _config["VNPay:TmnCode"] ?? "";
            var vnpHashSecret = _config["VNPay:HashSecret"] ?? "";
            var vnpReturnUrl = _config["VNPay:ReturnUrl"] ?? "";

            var vnp_Amount = (long)(amount * 100); // VNPay requires amount * 100

            // Force IP to avoid issues with IPv6 or missing IP
            ipAddress = "127.0.0.1";

            // Use Vietnam timezone for create/expire dates
            var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            var createDate = vietnamTime.ToString("yyyyMMddHHmmss");
            var expireDate = vietnamTime.AddMinutes(15).ToString("yyyyMMddHHmmss");

            // Truncate orderInfo to 100 chars max per VNPay spec
            if (orderInfo.Length > 100) orderInfo = orderInfo.Substring(0, 100);

            var vnpParams = new SortedDictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", vnpTmnCode },
                { "vnp_Amount", vnp_Amount.ToString() },
                { "vnp_CreateDate", createDate },
                { "vnp_CurrCode", "VND" },
                { "vnp_ExpireDate", expireDate },
                { "vnp_IpAddr", ipAddress },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", orderInfo },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", vnpReturnUrl },
                { "vnp_TxnRef", orderId.ToString() }
            };

            // Build sign data — values MUST be URL-encoded in the signature string
            var signData = string.Join("&", vnpParams.Select(kvp => $"{kvp.Key}={WebUtility.UrlEncode(kvp.Value)}"));
            var vnpSecureHash = ComputeHmacSha512(vnpHashSecret, signData);

            return $"{vnpUrl}?{signData}&vnp_SecureHash={vnpSecureHash}";
        }

        public async Task<PaymentDto> ValidateResponseAsync(IDictionary<string, string> queryParams)
        {
            var vnpHashSecret = _config["VNPay:HashSecret"] ?? "";

            // Extract secure hash from response
            queryParams.TryGetValue("vnp_SecureHash", out var vnpSecureHash);
            queryParams.TryGetValue("vnp_TxnRef", out var vnpTxnRef);
            queryParams.TryGetValue("vnp_ResponseCode", out var vnpResponseCode);
            queryParams.TryGetValue("vnp_TransactionNo", out var vnpTransactionNo);
            queryParams.TryGetValue("vnp_BankCode", out var vnpBankCode);
            queryParams.TryGetValue("vnp_Amount", out var vnpAmount);

            // Remove hash params for signature verification
            var signParams = new SortedDictionary<string, string>(queryParams);
            signParams.Remove("vnp_SecureHash");
            signParams.Remove("vnp_SecureHashType");

            // Compute expected hash — values MUST be URL-encoded to match VNPay's signature
            var signData = string.Join("&", signParams.Select(x => $"{x.Key}={WebUtility.UrlEncode(x.Value)}"));
            var expectedHash = ComputeHmacSha512(vnpHashSecret, signData);

            var isValid = vnpSecureHash?.Equals(expectedHash, StringComparison.InvariantCultureIgnoreCase) == true;
            var isPaid = isValid && vnpResponseCode == "00";

            var paymentDto = new PaymentDto
            {
                OrderId = int.TryParse(vnpTxnRef, out var id) ? id : 0,
                TransactionRef = vnpTransactionNo,
                BankCode = vnpBankCode,
                VnPayResponseCode = vnpResponseCode,
                Status = isPaid ? PaymentStatus.Paid : PaymentStatus.Failed,
                Amount = decimal.TryParse(vnpAmount, out var amt) ? amt / 100 : 0,
                PaidAt = isPaid ? DateTime.UtcNow : null
            };

            return await Task.FromResult(paymentDto);
        }

        public async Task<bool> UpdatePaymentAsync(int orderId, PaymentDto paymentDto)
        {
            var payments = await _unitOfWork.Payments.FindAsync(p => p.OrderId == orderId);
            var payment = payments.FirstOrDefault();

            if (payment == null) return false;

            payment.TransactionRef = paymentDto.TransactionRef;
            payment.BankCode = paymentDto.BankCode;
            payment.VnPayResponseCode = paymentDto.VnPayResponseCode;
            payment.Status = paymentDto.Status;
            payment.PaidAt = paymentDto.PaidAt;
            payment.VnPayResponseData = System.Text.Json.JsonSerializer.Serialize(paymentDto);

            _unitOfWork.Payments.Update(payment);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        private static string ComputeHmacSha512(string key, string data)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using var hmac = new HMACSHA512(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
