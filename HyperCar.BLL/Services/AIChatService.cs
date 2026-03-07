using Google.GenAI;
using Google.GenAI.Types;
using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using HyperCar.DAL.Entities;
using HyperCar.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Text;
using System.Text.RegularExpressions;

namespace HyperCar.BLL.Services
{
    public class AIChatService : IAIChatService
    {
        // ===== Dependencies =====
        private readonly IUnitOfWork _unitOfWork;
        private readonly Client _geminiClient;                // Google.GenAI SDK Client (singleton)
        private readonly IMemoryCache _cache;                  // Cache danh sách xe tránh query DB mỗi request
        private readonly ILogger<AIChatService> _logger;
        private readonly IConfiguration _config;

        // ===== Constants =====
        private const string CarCacheKey = "HyperCar_ActiveCars";
        private const int CacheDurationMinutes = 10;
        private const int MaxHistoryMessages = 5;              // Số lượng tin nhắn lịch sử gửi kèm prompt
        private const int MaxRelevantCars = 8;                 // Số xe tối đa gửi vào context
        private const int LevenshteinThreshold = 3;            // Ngưỡng fuzzy match

        // ===== Polly Resilience Pipeline =====
        // Retry 3 lần với exponential backoff: 2s → 4s → 8s
        private readonly ResiliencePipeline<GenerateContentResponse> _resiliencePipeline;

        public AIChatService(
            IUnitOfWork unitOfWork,
            Client geminiClient,
            IMemoryCache cache,
            ILogger<AIChatService> logger,
            IConfiguration config)
        {
            _unitOfWork = unitOfWork;
            _geminiClient = geminiClient;
            _cache = cache;
            _logger = logger;
            _config = config;

            // Khởi tạo Polly pipeline — retry 3x exponential backoff
            _resiliencePipeline = new ResiliencePipelineBuilder<GenerateContentResponse>()
                .AddRetry(new RetryStrategyOptions<GenerateContentResponse>
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromSeconds(2),               // 2s → 4s → 8s
                    ShouldHandle = new PredicateBuilder<GenerateContentResponse>()
                        .Handle<Exception>(),                       // Catch tất cả exception từ SDK
                    OnRetry = args =>
                    {
                        _logger.LogWarning(
                            "Gemini API retry #{Attempt} after {Delay}s — Reason: {Reason}",
                            args.AttemptNumber + 1,
                            args.RetryDelay.TotalSeconds,
                            args.Outcome.Exception?.Message ?? "Unknown");
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        #region ===== PUBLIC METHODS =====

        /// <summary>
        /// Entry point chính — RAG pipeline hoàn chỉnh:
        /// Message → DB Query → Fuzzy Match → Context → Gemini SDK → Save History → Response
        /// </summary>
        public async Task<ChatMessageDto> SendMessageAsync(ChatRequestDto request)
        {
            try
            {
                // Step 1: Trích xuất ngân sách và từ khóa từ câu hỏi
                var (budget, keywords) = ExtractBudgetAndKeywords(request.Message);

                // Step 2: Lấy danh sách xe từ cache/DB
                var allCars = await GetCarContextAsync();

                // Step 3: Fuzzy match — tìm xe phù hợp nhất
                var relevantCars = FuzzyMatchCars(allCars, request.Message, budget, keywords);

                // Step 4: Build SystemInstruction (SDK-native Content object)
                var systemInstruction = BuildSystemInstruction(relevantCars, allCars);

                // Step 5: Build chat history (SDK-native List<Content>)
                var chatHistory = await BuildChatHistory(request.UserId, request.SessionId);

                // Step 6: Gọi Gemini API qua SDK + Polly retry
                var aiResponse = await CallGeminiAsync(systemInstruction, chatHistory, request.Message);

                // Step 7: Format response — đảm bảo có links và giá in đậm
                aiResponse = FormatResponseMarkdown(aiResponse);

                // Step 8: Lưu lịch sử hội thoại
                var history = new ConversationHistory
                {
                    UserId = request.UserId,
                    SessionId = request.SessionId,
                    UserMessage = request.Message,
                    AiResponse = aiResponse,
                    CreatedDate = DateTime.UtcNow
                };
                await _unitOfWork.ConversationHistories.AddAsync(history);
                await _unitOfWork.SaveChangesAsync();

                return new ChatMessageDto
                {
                    UserMessage = request.Message,
                    AiResponse = aiResponse,
                    CreatedDate = history.CreatedDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI Chat pipeline failed for message: {Message}", request.Message);
                return HandleGeminiException(request.Message, ex);
            }
        }

        /// <summary>
        /// Lấy lịch sử hội thoại từ database
        /// </summary>
        public async Task<IEnumerable<ChatMessageDto>> GetHistoryAsync(string? userId, string? sessionId, int count = 20)
        {
            var query = _unitOfWork.ConversationHistories.Query();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(c => c.UserId == userId);
            else if (!string.IsNullOrEmpty(sessionId))
                query = query.Where(c => c.SessionId == sessionId);
            else
                return Enumerable.Empty<ChatMessageDto>();

            var history = await query
                .OrderByDescending(c => c.CreatedDate)
                .Take(count)
                .ToListAsync();

            return history.OrderBy(c => c.CreatedDate).Select(c => new ChatMessageDto
            {
                UserMessage = c.UserMessage,
                AiResponse = c.AiResponse,
                CreatedDate = c.CreatedDate
            });
        }

        #endregion

        #region ===== PRIVATE: RAG — DATABASE RETRIEVAL =====

        /// <summary>
        /// Lấy danh sách xe từ IMemoryCache (10 phút TTL)
        /// Nếu cache miss → query DB và cache lại
        /// </summary>
        private async Task<List<Car>> GetCarContextAsync()
        {
            if (_cache.TryGetValue(CarCacheKey, out List<Car>? cachedCars) && cachedCars != null)
                return cachedCars;

            var cars = await _unitOfWork.Cars.Query()
                .Include(c => c.Brand)
                .Where(c => c.IsActive)
                .AsNoTracking()
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheDurationMinutes))
                .SetPriority(CacheItemPriority.High);

            _cache.Set(CarCacheKey, cars, cacheOptions);

            _logger.LogInformation("Car cache refreshed — {Count} active cars loaded", cars.Count);
            return cars;
        }

        /// <summary>
        /// Trích xuất ngân sách (giá) và từ khóa từ câu hỏi người dùng
        /// Hỗ trợ cả tiếng Việt và tiếng Anh
        /// Ví dụ: "xe dưới 3 triệu đô" → budget = 3_000_000, keywords = ["xe"]
        /// </summary>
        private (decimal? budget, List<string> keywords) ExtractBudgetAndKeywords(string message)
        {
            var msg = message.ToLower().Trim();
            decimal? budget = null;
            var keywords = new List<string>();

            // === Trích xuất ngân sách ===
            // Pattern: "dưới X triệu" / "under X million" / "tầm X triệu đô"
            var budgetPatterns = new[]
            {
                @"(\d+(?:[.,]\d+)?)\s*(?:triệu|million)\s*(?:đô|usd|đô la|\$)?",
                @"(?:dưới|under|below|tầm|khoảng|around|budget)\s*\$?\s*(\d+(?:[.,]\d+)?)\s*(?:k|K|nghìn|thousand)?",
                @"\$\s*(\d+(?:[.,]\d+)?)\s*(?:k|K|m|M)?",
                @"(\d{1,3}(?:,\d{3})+)\s*(?:usd|\$|đô)",
            };

            foreach (var pattern in budgetPatterns)
            {
                var match = Regex.Match(msg, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var numStr = match.Groups[1].Value.Replace(",", "").Replace(".", "");
                    if (decimal.TryParse(numStr, out var num))
                    {
                        // Tự động detect đơn vị: < 100 = triệu USD, < 100K = nghìn USD
                        budget = num < 100 ? num * 1_000_000m
                               : num < 100_000 ? num * 1_000m
                               : num;
                        break;
                    }
                }
            }

            // === Trích xuất từ khóa ===
            // Loại bỏ stop words tiếng Việt và tiếng Anh
            var stopWords = new HashSet<string>
            {
                "tôi", "tui", "mình", "em", "anh", "chị", "muốn", "cần", "tìm", "hỏi",
                "cho", "về", "với", "của", "và", "hay", "hoặc", "là", "có", "không",
                "the", "a", "an", "is", "are", "do", "does", "i", "want", "need", "me",
                "what", "which", "how", "much", "can", "you", "show", "tell", "please",
                "xe", "car", "cars", "chiếc", "mẫu", "dòng", "loại", "nào", "gì",
                "bao", "nhiêu", "được", "ạ", "nhé", "nha", "vậy", "thế"
            };

            var words = Regex.Split(msg, @"\s+")
                .Where(w => w.Length > 2 && !stopWords.Contains(w))
                .Distinct()
                .ToList();

            keywords.AddRange(words);

            return (budget, keywords);
        }

        /// <summary>
        /// Fuzzy matching — tìm xe phù hợp nhất bằng scoring system + Levenshtein distance
        /// Scoring: Brand match +10, Name match +15, Levenshtein +8, Budget +7, Category +5, etc.
        /// </summary>
        private List<Car> FuzzyMatchCars(List<Car> allCars, string message, decimal? budget, List<string> keywords)
        {
            var msg = message.ToLower();
            var scored = new Dictionary<Car, int>();

            foreach (var car in allCars)
            {
                int score = 0;
                var carName = car.Name.ToLower();
                var brandName = car.Brand?.Name?.ToLower() ?? "";

                // === Exact match — ưu tiên cao nhất ===
                if (msg.Contains(carName) || carName.Contains(msg))
                    score += 15;

                // === Brand match ===
                if (!string.IsNullOrEmpty(brandName) && msg.Contains(brandName))
                    score += 10;

                // === Partial word match ===
                var carWords = carName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in carWords)
                {
                    if (word.Length > 2 && msg.Contains(word))
                        score += 4;
                }

                // === Levenshtein fuzzy match — cho phép typo ===
                foreach (var keyword in keywords)
                {
                    // So sánh từ khóa với tên xe và thương hiệu
                    foreach (var carWord in carWords)
                    {
                        if (carWord.Length > 2 && LevenshteinDistance(keyword, carWord) <= LevenshteinThreshold)
                            score += 8;
                    }

                    // So sánh với brand
                    var brandWords = brandName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var bw in brandWords)
                    {
                        if (bw.Length > 2 && LevenshteinDistance(keyword, bw) <= LevenshteinThreshold)
                            score += 8;
                    }
                }

                // === Budget match — xe nằm trong tầm giá ±30% ===
                if (budget.HasValue && budget.Value > 0)
                {
                    if (car.Price <= budget.Value * 1.3m && car.Price >= budget.Value * 0.5m)
                        score += 7;
                    if (car.Price <= budget.Value)
                        score += 3;  // Bonus nếu nằm trong ngân sách
                }

                // === Price keywords ===
                if (Regex.IsMatch(msg, @"(giá|price|tiền|rẻ|đắt|cheap|expensive|mắc|bao nhiêu|budget)"))
                    score += 1;

                // === Performance keywords ===
                if (Regex.IsMatch(msg, @"(nhanh|fast|speed|tốc độ|mạnh|powerful|hp|mã lực|horsepower|turbo|v8|v10|v12|w16|hybrid|điện|electric)"))
                    score += 2;

                // === Category match ===
                if (car.Category != null)
                {
                    var cat = car.Category.ToLower();
                    if (msg.Contains(cat)
                        || (msg.Contains("suv") && cat.Contains("suv"))
                        || (msg.Contains("sport") && cat.Contains("sport"))
                        || (msg.Contains("sedan") && cat.Contains("sedan"))
                        || (msg.Contains("coupe") && cat.Contains("coupe"))
                        || (msg.Contains("hypercar") && cat.Contains("hypercar"))
                        || (msg.Contains("supercar") && cat.Contains("supercar")))
                        score += 5;
                }

                // === Stock availability ===
                if (Regex.IsMatch(msg, @"(còn hàng|có sẵn|available|in stock|tồn kho|còn không)") && car.Stock > 0)
                    score += 3;

                if (score > 0)
                    scored[car] = score;
            }

            // Trả về top kết quả, sắp xếp theo điểm giảm dần
            var results = scored.OrderByDescending(kv => kv.Value)
                                .Take(MaxRelevantCars)
                                .Select(kv => kv.Key)
                                .ToList();

            // Nếu không match gì → trả về top xe mới nhất
            if (!results.Any())
                results = allCars.OrderByDescending(c => c.CreatedDate).Take(6).ToList();

            return results;
        }

        /// <summary>
        /// Thuật toán Levenshtein Distance — đo khoảng cách chỉnh sửa giữa 2 chuỗi
        /// Dùng để fuzzy match tên xe/brand khi khách gõ sai chính tả
        /// Ví dụ: "lamborgini" ↔ "lamborghini" = distance 1
        /// </summary>
        private static int LevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
            if (string.IsNullOrEmpty(target)) return source.Length;

            var sourceLen = source.Length;
            var targetLen = target.Length;
            var matrix = new int[sourceLen + 1, targetLen + 1];

            for (var i = 0; i <= sourceLen; i++) matrix[i, 0] = i;
            for (var j = 0; j <= targetLen; j++) matrix[0, j] = j;

            for (var i = 1; i <= sourceLen; i++)
            {
                for (var j = 1; j <= targetLen; j++)
                {
                    var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[sourceLen, targetLen];
        }

        #endregion

        #region ===== PRIVATE: GEMINI SDK — PROMPT BUILDING =====

        /// <summary>
        /// Build SystemInstruction dùng SDK-native Content object
        /// KHÔNG concatenate vào user message — đặt đúng vị trí SystemInstruction
        /// 
        /// Persona: Alex — Director of HyperCar Luxury Consulting
        /// Ngôn ngữ: Tiếng Việt, xưng "Em", gọi khách "Anh/Chị"
        /// </summary>
        private Content BuildSystemInstruction(List<Car> relevantCars, List<Car> allCars)
        {
            var sb = new StringBuilder();

            // === PERSONA ===
            sb.AppendLine(@"Bạn là Alex — Giám đốc Tư vấn Cao cấp tại HyperCar, showroom siêu xe hàng đầu Việt Nam.
Bạn có hơn 15 năm kinh nghiệm tư vấn xe sang cho khách hàng thượng lưu.

TÍNH CÁCH & PHONG CÁCH:
- Chuyên nghiệp, tinh tế, đẳng cấp — như một luxury consultant thực thụ
- Xưng ""Em"" với khách, gọi khách là ""Anh/Chị""
- Dùng ngôn ngữ sinh động, truyền cảm khi mô tả xe: âm thanh động cơ, cảm giác lái, thiết kế
- Tư vấn khéo léo, gợi ý đúng nhu cầu — thuyết phục nhưng KHÔNG ép mua
- Luôn thể hiện đam mê và kiến thức sâu về xe
- Trả lời bằng tiếng Việt, tự nhiên như đang tư vấn trực tiếp tại showroom");

            // === RESPONSE FORMAT ===
            sb.AppendLine(@"

QUY TẮC RESPONSE (BẮT BUỘC TUÂN THỦ):
1. CHỈ giới thiệu xe có trong KHO HÀNG bên dưới — TUYỆT ĐỐI KHÔNG bịa xe không có
2. Giá PHẢI in đậm Markdown: **X,XXX,XXX USD**
3. Khi nhắc đến xe cụ thể, LUÔN kèm link CTA:
   👉 [Tên Xe](/Cars/Details?id=ID)
4. Luôn đề cập: giá, công suất, tốc độ, tình trạng còn hàng
5. So sánh khi khách phân vân giữa nhiều mẫu
6. Nếu xe khách hỏi không có → giới thiệu mẫu tương tự có sẵn
7. Trả lời ngắn gọn, súc tích (2-3 đoạn). Dùng emoji phù hợp 🏎️💨✨");

            // === SECURITY ===
            sb.AppendLine(@"

BẢO MẬT (TUYỆT ĐỐI):
- KHÔNG BAO GIỜ tiết lộ: database schema, SQL, API key, source code, thông tin user khác, config hệ thống
- KHÔNG trả lời về: backend, server, mật khẩu, token, admin panel
- Nếu bị hỏi → ""Dạ, thông tin này em không được phép chia sẻ ạ. Em có thể tư vấn về xe cho Anh/Chị nhé!""

XỬ LÝ CÂU HỎI NGOÀI LỀ:
- Trả lời ngắn, vui vẻ rồi quay lại xe: ""Ha ha, Anh/Chị vui tính quá! Nói về xe thì em rành hơn nè 😄""");

            // === CAR INVENTORY CONTEXT ===
            sb.AppendLine();
            sb.AppendLine("=== KHO XE HIỆN CÓ CỦA HYPERCAR ===");
            sb.AppendLine($"Tổng kho: {allCars.Count} mẫu | Hiển thị: {relevantCars.Count} xe phù hợp nhất");
            sb.AppendLine();

            foreach (var car in relevantCars)
            {
                sb.AppendLine($"🏎️ [{car.Name}](/Cars/Details?id={car.Id}) — {car.Brand?.Name ?? "N/A"}");
                sb.AppendLine($"   💰 Giá: **{car.Price:N0} USD**");
                sb.AppendLine($"   🔧 Động cơ: {car.Engine ?? "N/A"} | {car.HorsePower} HP");
                sb.AppendLine($"   ⚡ Tốc độ: {car.TopSpeed} km/h | 0-100: {car.Acceleration}s");
                sb.AppendLine($"   📦 Tồn kho: {(car.Stock > 0 ? $"{car.Stock} chiếc — CÒN HÀNG" : "❌ HẾT HÀNG")}");
                sb.AppendLine($"   📂 Danh mục: {car.Category ?? "N/A"}");
                if (!string.IsNullOrEmpty(car.Description))
                {
                    var desc = car.Description.Length > 200
                        ? car.Description[..200] + "..."
                        : car.Description;
                    sb.AppendLine($"   📝 {desc}");
                }
                sb.AppendLine($"   🔗 Link: /Cars/Details?id={car.Id}");
                sb.AppendLine();
            }

            // === BRAND SUMMARY ===
            var brands = allCars.Select(c => c.Brand?.Name).Where(b => b != null).Distinct().ToList();
            sb.AppendLine($"Thương hiệu có sẵn: {string.Join(", ", brands)}");

            if (allCars.Any())
            {
                sb.AppendLine($"Tầm giá: {allCars.Min(c => c.Price):N0} — {allCars.Max(c => c.Price):N0} USD");
            }

            // Trả về SDK-native Content object cho SystemInstruction
            return new Content
            {
                Parts = new List<Part>
                {
                    new Part { Text = sb.ToString() }
                }
            };
        }

        /// <summary>
        /// Build chat history dùng SDK-native Content objects với Role "user" / "model"
        /// KHÔNG dùng StringBuilder — dùng đúng cấu trúc SDK yêu cầu
        /// </summary>
        private async Task<List<Content>> BuildChatHistory(string? userId, string? sessionId)
        {
            var history = new List<Content>();
            var query = _unitOfWork.ConversationHistories.Query();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(c => c.UserId == userId);
            else if (!string.IsNullOrEmpty(sessionId))
                query = query.Where(c => c.SessionId == sessionId);
            else
                return history;

            var recent = await query
                .OrderByDescending(c => c.CreatedDate)
                .Take(MaxHistoryMessages)
                .AsNoTracking()
                .ToListAsync();

            // Sắp xếp theo thời gian tăng dần (cũ → mới)
            foreach (var msg in recent.OrderBy(c => c.CreatedDate))
            {
                // Tin nhắn của khách — Role "user"
                history.Add(new Content
                {
                    Role = "user",
                    Parts = new List<Part> { new Part { Text = msg.UserMessage } }
                });

                // Phản hồi của AI — Role "model"
                var truncatedResponse = msg.AiResponse.Length > 500
                    ? msg.AiResponse[..500] + "..."
                    : msg.AiResponse;

                history.Add(new Content
                {
                    Role = "model",
                    Parts = new List<Part> { new Part { Text = truncatedResponse } }
                });
            }

            return history;
        }

        #endregion

        #region ===== PRIVATE: GEMINI SDK — API CALL =====

        /// <summary>
        /// Gọi Gemini API thông qua Google.GenAI SDK chính thức
        /// Wrapped với Polly retry pipeline cho fault tolerance
        /// 
        /// Config: Temperature=0.7, MaxOutputTokens=2048, TopP=0.95, TopK=40
        /// Safety: Tất cả categories → OFF (không block racing slang)
        /// SystemInstruction: SDK-native (không concat vào message)
        /// </summary>
        private async Task<string> CallGeminiAsync(Content systemInstruction, List<Content> chatHistory, string userMessage)
        {
            var model = _config["Gemini:Model"] ?? "gemini-2.0-flash";

            // Build danh sách contents: history + tin nhắn mới
            var contents = new List<Content>(chatHistory)
            {
                new Content
                {
                    Role = "user",
                    Parts = new List<Part> { new Part { Text = userMessage } }
                }
            };

            // Cấu hình generation — tuned cho luxury sales consultant
            var generateConfig = new GenerateContentConfig
            {
                // SystemInstruction đặt đúng vị trí SDK — KHÔNG concat vào prompt
                SystemInstruction = systemInstruction,

                // Generation parameters
                Temperature = 0.7f,
                MaxOutputTokens = 2048,
                TopP = 0.95f,
                TopK = 40,

                // Safety settings — tắt tất cả filter (không block racing terminology)
                SafetySettings = new List<SafetySetting>
                {
                    new SafetySetting { Category = HarmCategory.HarmCategoryHarassment, Threshold = HarmBlockThreshold.Off },
                    new SafetySetting { Category = HarmCategory.HarmCategoryHateSpeech, Threshold = HarmBlockThreshold.Off },
                    new SafetySetting { Category = HarmCategory.HarmCategorySexuallyExplicit, Threshold = HarmBlockThreshold.Off },
                    new SafetySetting { Category = HarmCategory.HarmCategoryDangerousContent, Threshold = HarmBlockThreshold.Off },
                }
            };

            // Gọi Gemini API qua Polly resilience pipeline
            var response = await _resiliencePipeline.ExecuteAsync(async cancellationToken =>
            {
                return await _geminiClient.Models.GenerateContentAsync(
                    model: model,
                    contents: contents,
                    config: generateConfig
                );
            });

            // Trích xuất text từ response
            var text = response?.Candidates?.FirstOrDefault()?.Content?.Parts
                ?.Where(p => !string.IsNullOrEmpty(p.Text))
                .Select(p => p.Text)
                .Aggregate(new StringBuilder(), (sb, t) => sb.Append(t))
                .ToString();

            return !string.IsNullOrEmpty(text)
                ? text
                : "Em sẵn sàng tư vấn cho Anh/Chị! Anh/Chị đang quan tâm đến dòng xe nào ạ? 🏎️";
        }

        #endregion

        #region ===== PRIVATE: POST-PROCESSING =====

        /// <summary>
        /// Đảm bảo response tuân thủ format Markdown:
        /// - Giá phải in đậm: **X,XXX,XXX USD**
        /// - Link phải đúng format: [Tên](/Cars/Details?id=X)
        /// </summary>
        private static string FormatResponseMarkdown(string response)
        {
            if (string.IsNullOrEmpty(response)) return response;

            // Đảm bảo giá USD được in đậm (nếu chưa)
            response = Regex.Replace(
                response,
                @"(?<!\*\*)(\d{1,3}(?:,\d{3})+)\s*USD(?!\*\*)",
                "**$1 USD**");

            return response;
        }

        /// <summary>
        /// Xử lý exception — trả về tin nhắn thân thiện bằng tiếng Việt
        /// KHÔNG BAO GIỜ expose internal error ra ngoài
        /// </summary>
        private ChatMessageDto HandleGeminiException(string userMessage, Exception ex)
        {
            // Log chi tiết nhưng KHÔNG trả về cho user
            var errorType = ex.GetType().Name;
            _logger.LogError(ex, "Gemini exception [{ErrorType}]: {Message}", errorType, ex.Message);

            // Tin nhắn fallback — thân thiện, không lộ thông tin kỹ thuật
            var friendlyMessage = ex switch
            {
                // Timeout / DeadlineExceeded
                _ when ex.Message.Contains("deadline", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                    => "Hiện tại hệ thống tư vấn đang rất bận, Anh/Chị vui lòng thử lại sau vài giây giúp em nhé 🙏",

                // Quota exceeded
                _ when ex.Message.Contains("quota", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("429", StringComparison.OrdinalIgnoreCase)
                    => "Em đang nhận rất nhiều yêu cầu tư vấn, Anh/Chị vui lòng chờ em một chút rồi thử lại nhé! 😊",

                // Safety blocked
                _ when ex.Message.Contains("safety", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("blocked", StringComparison.OrdinalIgnoreCase)
                    => "Dạ, em không thể trả lời câu hỏi này ạ. Anh/Chị có muốn tìm hiểu về xe không ạ? 🏎️",

                // Default fallback
                _ => "Xin lỗi Anh/Chị, em gặp chút trục trặc kỹ thuật. Anh/Chị vui lòng thử lại sau giây lát, hoặc xem bộ sưu tập xe tại [HyperCar Collection](/Cars) nhé! 🏎️"
            };

            return new ChatMessageDto
            {
                UserMessage = userMessage,
                AiResponse = friendlyMessage,
                CreatedDate = DateTime.UtcNow
            };
        }

        #endregion
    }
}
