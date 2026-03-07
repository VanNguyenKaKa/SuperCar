using HyperCar.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace HyperCar.BLL.Services
{
    public class ShippingService : IShippingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<ShippingService> _logger;

        // Cache address data
        private static List<ProvinceDto>? _cachedProvinces;
        private static readonly Dictionary<int, List<DistrictDto>> _cachedDistricts = new();
        private static readonly Dictionary<int, List<WardDto>> _cachedWards = new();

        // Tier multipliers and labels
        private static readonly Dictionary<string, (decimal Multiplier, string Name, string Days)> Tiers = new()
        {
            ["standard"] = (1.0m, "Tiêu chuẩn", "5-7 ngày"),
            ["express"] = (1.5m, "Nhanh", "2-3 ngày"),
            ["hoatoc"] = (2.5m, "Hỏa tốc", "1-2 ngày")
        };

        public ShippingService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<ShippingService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("ViettelPost");
            _config = config;
            _logger = logger;
        }

        // ============================================================
        // SHIPPING FEE CALCULATION — with tier multiplier
        // ============================================================

        public async Task<ShippingFeeResult> CalculateFeeAsync(int senderProvinceId, int senderDistrictId,
            int receiverProvinceId, int receiverDistrictId, string shippingTier = "standard", decimal weight = 50)
        {
            var tier = Tiers.ContainsKey(shippingTier) ? shippingTier : "standard";
            var (multiplier, tierName, days) = Tiers[tier];

            decimal baseFee;

            try
            {
                var apiUrl = _config["ViettelPost:PriceUrl"] ?? "https://partner.viettelpost.vn/v2/order/getPriceAll";
                var token = _config["ViettelPost:Token"] ?? "";

                var requestData = new
                {
                    PRODUCT_WEIGHT = weight,
                    PRODUCT_PRICE = 0,
                    MONEY_COLLECTION = 0,
                    ORDER_SERVICE_ADD = "",
                    ORDER_SERVICE = "VCN",
                    SENDER_PROVINCE = senderProvinceId,
                    SENDER_DISTRICT = senderDistrictId,
                    RECEIVER_PROVINCE = receiverProvinceId,
                    RECEIVER_DISTRICT = receiverDistrictId,
                    PRODUCT_TYPE = "HH",
                    NATIONAL_TYPE = 1
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                request.Headers.Add("Token", token);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseBody);

                    if (doc.RootElement.TryGetProperty("data", out var dataArray)
                        && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in dataArray.EnumerateArray())
                        {
                            if (item.TryGetProperty("MA_DV_CHINH", out var serviceCode)
                                && item.TryGetProperty("GIA_CUOC", out var price))
                            {
                                var code = serviceCode.GetString();
                                if (code == "VCN" || code == "LCOD" || code == "VTK")
                                {
                                    baseFee = price.GetDecimal();
                                    return new ShippingFeeResult
                                    {
                                        Fee = baseFee * multiplier,
                                        Tier = tier,
                                        TierName = tierName,
                                        EstimatedDays = days
                                    };
                                }
                            }
                        }

                        foreach (var item in dataArray.EnumerateArray())
                        {
                            if (item.TryGetProperty("GIA_CUOC", out var price) && price.GetDecimal() > 0)
                            {
                                baseFee = price.GetDecimal();
                                return new ShippingFeeResult
                                {
                                    Fee = baseFee * multiplier,
                                    Tier = tier,
                                    TierName = tierName,
                                    EstimatedDays = days
                                };
                            }
                        }
                    }
                }

                _logger.LogWarning("ViettelPost getPriceAll returned no usable price. Status: {Status}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ViettelPost fee calculation failed. Using fallback rate.");
            }

            // Fallback: distance-based estimation
            baseFee = CalculateFallbackFee(senderProvinceId, receiverProvinceId);

            return new ShippingFeeResult
            {
                Fee = baseFee * multiplier,
                Tier = tier,
                TierName = tierName,
                EstimatedDays = days
            };
        }

        /// <summary>
        /// Fallback fee based on whether same city or inter-city
        /// </summary>
        private static decimal CalculateFallbackFee(int senderProvince, int receiverProvince)
        {
            if (senderProvince == receiverProvince)
                return 30_000m; // Same city
            return 50_000m; // Inter-city
        }

        // ============================================================
        // ADDRESS LOOKUP — provinces.open-api.vn (free, no token)
        // ============================================================

        /// <summary>
        /// Gets all 63 provinces/cities of Vietnam
        /// API: https://provinces.open-api.vn/api/
        /// </summary>
        public async Task<List<ProvinceDto>> GetProvincesAsync()
        {
            if (_cachedProvinces != null) return _cachedProvinces;

            try
            {
                var response = await _httpClient.GetAsync("https://provinces.open-api.vn/api/p/");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);

                    var provinces = new List<ProvinceDto>();
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        provinces.Add(new ProvinceDto
                        {
                            ProvinceId = item.GetProperty("code").GetInt32(),
                            ProvinceName = item.GetProperty("name").GetString() ?? ""
                        });
                    }

                    _cachedProvinces = provinces;
                    return provinces;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch provinces from open-api");
            }

            return GetFallbackProvinces();
        }

        /// <summary>
        /// Gets districts for a province
        /// API: https://provinces.open-api.vn/api/p/{code}?depth=2
        /// </summary>
        public async Task<List<DistrictDto>> GetDistrictsAsync(int provinceCode)
        {
            if (_cachedDistricts.TryGetValue(provinceCode, out var cached)) return cached;

            try
            {
                var response = await _httpClient.GetAsync($"https://provinces.open-api.vn/api/p/{provinceCode}?depth=2");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);

                    var districts = new List<DistrictDto>();
                    if (doc.RootElement.TryGetProperty("districts", out var districtArray))
                    {
                        foreach (var item in districtArray.EnumerateArray())
                        {
                            districts.Add(new DistrictDto
                            {
                                DistrictId = item.GetProperty("code").GetInt32(),
                                DistrictName = item.GetProperty("name").GetString() ?? "",
                                ProvinceId = provinceCode
                            });
                        }
                    }

                    _cachedDistricts[provinceCode] = districts;
                    return districts;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch districts for province {ProvinceCode}", provinceCode);
            }

            return new List<DistrictDto>();
        }

        /// <summary>
        /// Gets wards for a district
        /// API: https://provinces.open-api.vn/api/d/{code}?depth=2
        /// </summary>
        public async Task<List<WardDto>> GetWardsAsync(int districtCode)
        {
            if (_cachedWards.TryGetValue(districtCode, out var cached)) return cached;

            try
            {
                var response = await _httpClient.GetAsync($"https://provinces.open-api.vn/api/d/{districtCode}?depth=2");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);

                    var wards = new List<WardDto>();
                    if (doc.RootElement.TryGetProperty("wards", out var wardArray))
                    {
                        foreach (var item in wardArray.EnumerateArray())
                        {
                            wards.Add(new WardDto
                            {
                                WardId = item.GetProperty("code").GetInt32(),
                                WardName = item.GetProperty("name").GetString() ?? "",
                                DistrictId = districtCode
                            });
                        }
                    }

                    _cachedWards[districtCode] = wards;
                    return wards;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch wards for district {DistrictCode}", districtCode);
            }

            return new List<WardDto>();
        }

        // ============================================================
        // VIETTELPOST SHIPMENT CREATION & TRACKING
        // ============================================================

        public async Task<string?> CreateShipmentAsync(int orderId, string receiverName, string receiverPhone,
            string address, int provinceId, int districtId, string? wardCode)
        {
            try
            {
                var apiUrl = _config["ViettelPost:CreateOrderUrl"] ?? "https://partner.viettelpost.vn/v2/order/createOrder";
                var token = _config["ViettelPost:Token"] ?? "";

                var requestData = new
                {
                    ORDER_NUMBER = $"HC-{orderId:D8}",
                    GROUPADDRESS_ID = 0,
                    CUS_ID = 0,
                    DELIVERY_DATE = DateTime.Now.AddDays(3).ToString("dd/MM/yyyy HH:mm:ss"),
                    SENDER_FULLNAME = _config["ViettelPost:SenderName"] ?? "HyperCar Store",
                    SENDER_ADDRESS = _config["ViettelPost:SenderAddress"] ?? "",
                    SENDER_PHONE = _config["ViettelPost:SenderPhone"] ?? "",
                    RECEIVER_FULLNAME = receiverName,
                    RECEIVER_ADDRESS = address,
                    RECEIVER_PHONE = receiverPhone,
                    RECEIVER_PROVINCE = provinceId,
                    RECEIVER_DISTRICT = districtId,
                    PRODUCT_NAME = "HyperCar Order",
                    PRODUCT_WEIGHT = 50,
                    PRODUCT_TYPE = "HH",
                    ORDER_PAYMENT = 3,
                    ORDER_SERVICE = "VCN",
                    ORDER_NOTE = $"HyperCar Order #{orderId}"
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                request.Headers.Add("Token", token);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseBody);

                    if (doc.RootElement.TryGetProperty("data", out var data) &&
                        data.TryGetProperty("ORDER_NUMBER", out var trackingCode))
                    {
                        return trackingCode.GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create ViettelPost shipment for order {OrderId}", orderId);
            }

            return $"HC-{orderId:D8}";
        }

        public async Task<string?> TrackShipmentAsync(string trackingCode)
        {
            try
            {
                var apiUrl = _config["ViettelPost:TrackingUrl"] ?? "https://partner.viettelpost.vn/v2/order/tracking";
                var token = _config["ViettelPost:Token"] ?? "";

                var requestData = new { ORDER_NUMBER = trackingCode };
                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                request.Headers.Add("Token", token);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track shipment {TrackingCode}", trackingCode);
            }

            return null;
        }

        /// <summary>
        /// Fallback province list when API is unavailable
        /// </summary>
        private static List<ProvinceDto> GetFallbackProvinces()
        {
            return new List<ProvinceDto>
            {
                new() { ProvinceId = 1, ProvinceName = "Thành phố Hà Nội" },
                new() { ProvinceId = 79, ProvinceName = "Thành phố Hồ Chí Minh" },
                new() { ProvinceId = 48, ProvinceName = "Thành phố Đà Nẵng" },
                new() { ProvinceId = 31, ProvinceName = "Thành phố Hải Phòng" },
                new() { ProvinceId = 92, ProvinceName = "Thành phố Cần Thơ" },
            };
        }
    }
}
