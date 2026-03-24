using Google.GenAI;
using Google.GenAI.Types;
using HyperCar.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HyperCar.BLL.Services
{
    public class AIModerationService : IAIModerationService
    {
        private readonly Client _moderationClient;
        private readonly string _model;
        private readonly ILogger<AIModerationService> _logger;

        private static readonly string[] ProfanityRoots = new[]
        {
            // Core Vietnamese profanity
            "đụ", "địt", "đĩ", "đỉ", "cặc", "cặk", "lồn", "buồi",
            "dâm", "đéo", "đếch", "đ[eé]o", "vãi", "vl",
            // Insults
            "ngu", "khốn", "chó", "súc vật", "con điếm", "thằng ngu",
            "con ngu", "đồ ngu", "ngu như", "óc chó", "não chó",
            "mặt l[oồ]n", "mặt đ[ií]t", "thằng chó", "con chó",
            "đồ chó", "đồ khốn", "thằng khốn", "con khốn",
            "mẹ mày", "má mày", "bố mày", "cha mày",
            "đm", "vcl", "vkl", "wtf", "stfu", "fck", "f[uư]ck",
            "bitch", "bastard", "shit", "ass ?hole", "dick",
            "con mẹ", "đ[uụ] m[aáeẹ]", "đ[ií]t m[eẹ]",
            // Degrading
            "như thú", "như chó", "như lợn", "như heo", "như cứt",
            "rác rưởi", "ghê tởm", "kinh tởm", "đáng khinh",
            // Sexual
            "dâm", "sex", "porn", "xxx",
        };

        private static readonly Regex ProfanityRegex = BuildProfanityRegex();

        private static readonly Regex SpamUrlRegex = new(
            @"(https?://|www\.|bit\.ly|tinyurl|t\.co|goo\.gl|shorturl|click\s*here|link\s*:)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RepeatedCharRegex = new(
            @"(.)\1{7,}",
            RegexOptions.Compiled);
        private static readonly Regex AllCapsSpamRegex = new(
            @"[A-ZÀÁẠẢÃĂẮẰẶẲẴÂẤẦẬẨẪĐÈÉẸẺẼÊẾỀỆỂỄÌÍỊỈĨÒÓỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠÙÚỤỦŨƯỨỪỰỬỮỲÝỴỶỸ]{10,}",
            RegexOptions.Compiled);

        private static Regex BuildProfanityRegex()
        {
            var patterns = new List<string>();
            // Optional separator: any of . ! @ # * - _ space , ; : (0 or more)
            // NOTE: Inside [...], most chars don't need escaping. Put - at end.
            const string sep = @"[\s.!@#*_,;:-]*";

            foreach (var word in ProfanityRoots)
            {
                if (word.Length < 2) continue;

                // If the word contains regex special patterns (like [uụ]), use it as-is
                if (word.Contains('[') || word.Contains('?') || word.Contains('|'))
                {
                    patterns.Add(word);
                    continue;
                }

                // For multi-word phrases (with spaces), split by space and join with flexible separator
                if (word.Contains(' '))
                {
                    var parts = word.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var escaped = parts.Select(p => Regex.Escape(p));
                    patterns.Add(string.Join(@"\s+", escaped));
                    continue;
                }

                // Insert optional separator between each character
                // "đụ" → "đ[\s.!@#*_,;:-]*ụ"
                var chars = new List<string>();
                var enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(word);
                while (enumerator.MoveNext())
                {
                    chars.Add(Regex.Escape(enumerator.GetTextElement()));
                }
                patterns.Add(string.Join(sep, chars));
            }

            // Use (?: ) non-capturing group; no \b — it doesn't work with Vietnamese Unicode
            var combinedPattern = @"(?:" + string.Join("|", patterns) + @")";
            return new Regex(combinedPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
        private static string? RunPreFilter(string comment)
        {
            // Normalize: lowercase + remove diacritics-like obfuscation
            var normalized = comment.ToLowerInvariant();

            // Check profanity
            var profanityMatch = ProfanityRegex.Match(normalized);
            if (profanityMatch.Success)
                return $"Ngôn từ không phù hợp được phát hiện: \"{profanityMatch.Value}\"";

            // Check spam URLs
            if (SpamUrlRegex.IsMatch(comment))
                return "Phát hiện link spam/quảng cáo.";

            // Check repeated character spam
            if (RepeatedCharRegex.IsMatch(comment))
                return "Phát hiện spam ký tự lặp lại quá nhiều.";

            // Check all-caps spam
            if (AllCapsSpamRegex.IsMatch(comment) && comment.Length > 15)
                return "Phát hiện spam chữ in hoa.";

            return null; // Clean
        }

        // ══════════════════════════════════════════════════════════
        // LAYER 2: GEMINI AI MODERATION (stricter prompt)
        // ══════════════════════════════════════════════════════════

        private const string SystemPrompt = @"Bạn là hệ thống kiểm duyệt nội dung NGHIÊM NGẶT cho website thương mại điện tử bán siêu xe cao cấp.

NHIỆM VỤ: Phân tích bình luận đánh giá sản phẩm và PHÁT HIỆN tất cả nội dung vi phạm.

CÁC LOẠI VI PHẠM PHẢI PHÁT HIỆN:
1. **Tục tĩu / Profanity** — TẤT CẢ ngôn từ thô tục, chửi bậy, xúc phạm bằng tiếng Việt hoặc tiếng Anh. Bao gồm cả từ bị biến thể/che giấu (thêm dấu chấm, dấu cách, ký tự đặc biệt giữa các chữ cái, viết tắt).
   Ví dụ: đụ, địt, cặc, lồn, đéo, vãi, đm, vcl, vkl, f*ck, sh!t, và TẤT CẢ biến thể.

2. **Lăng mạ / Xúc phạm** — Mọi hình thức xúc phạm, chê bai CON NGƯỜI (không phải sản phẩm). So sánh người với động vật (""ngu như chó"", ""như thú""), gọi tên miệt thị, chửi gia đình.
   Ví dụ: ""ngu"", ""óc chó"", ""như thú"", ""đồ rác"", ""mẹ mày"", ""thằng ngu""

3. **Spam / Quảng cáo** — Link lạ, URL, quảng cáo sản phẩm/dịch vụ khác, nội dung lặp lại vô nghĩa.
   Ví dụ: ""mua xe giá rẻ tại..."", ""click vào link..."", chuỗi ký tự vô nghĩa lặp lại.

4. **Nội dung độc hại** — Đe dọa, quấy rối, kỳ thị, kích động bạo lực, nội dung người lớn.

5. **Ngôn từ mỉa mai/châm biếm ác ý** — Bình luận dùng ngôn từ mỉa mai để xúc phạm người khác hoặc sản phẩm một cách ác ý, không mang tính xây dựng.
   Ví dụ: ""đậu xanh rau chuối"" (dùng như cách nói xấu/chế giễu), ngôn từ bóng gió xúc phạm.

QUY TẮC QUAN TRỌNG:
- Bình luận ngắn hợp lệ (""Xe đẹp"", ""OK"", ""Tốt"", ""Không thích lắm"") → isClean = true
- Chê sản phẩm LỊCH SỰ (""Xe chạy ồn"", ""Giá hơi cao"", ""Nội thất chưa tốt"") → isClean = true
- Khi NGHI NGỜ nội dung có ý xấu → isClean = false (ưu tiên an toàn)
- Từ tiếng lóng Việt Nam mang tính xúc phạm → isClean = false
- Nội dung hỗn hợp (có cả lời khen và lời chửi) → isClean = false

BẮT BUỘC: Trả lời ĐÚNG 1 JSON object, KHÔNG có text nào khác:
{""isClean"": true/false, ""reason"": ""lý do cụ thể nếu không clean, để trống nếu clean""}";

        public AIModerationService(IConfiguration configuration, ILogger<AIModerationService> logger)
        {
            _logger = logger;

            var apiKey = configuration["Gemini:ModerationApiKey"]
                ?? configuration["Gemini:ApiKey"]
                ?? throw new InvalidOperationException("Gemini:ModerationApiKey is required.");

            _model = configuration["Gemini:Model"] ?? "gemini-2.0-flash";
            _moderationClient = new Client(apiKey: apiKey);
        }

        public async Task<ModerationResult> AnalyzeReviewAsync(string comment)
        {
            if (string.IsNullOrWhiteSpace(comment) || comment.Length < 2)
                return ModerationResult.Clean();

            // ── LAYER 1: Local regex pre-filter ──
            var preFilterReason = RunPreFilter(comment);
            if (preFilterReason != null)
            {
                _logger.LogInformation("Pre-filter flagged comment: {Reason}", preFilterReason);
                return ModerationResult.Flagged(preFilterReason);
            }

            // ── LAYER 2: Gemini AI moderation ──
            try
            {
                var systemInstruction = new Content
                {
                    Parts = new List<Part> { new Part { Text = SystemPrompt } }
                };

                var contents = new List<Content>
                {
                    new Content
                    {
                        Role = "user",
                        Parts = new List<Part> { new Part { Text = $"Phân tích bình luận sau:\n\"{comment}\"" } }
                    }
                };

                var config = new GenerateContentConfig
                {
                    SystemInstruction = systemInstruction,
                    Temperature = 0.05f,        // Very low — deterministic moderation
                    MaxOutputTokens = 256,
                    TopP = 0.8f,
                    ResponseMimeType = "application/json",
                    SafetySettings = new List<SafetySetting>
                    {
                        new SafetySetting { Category = HarmCategory.HarmCategoryHarassment, Threshold = HarmBlockThreshold.Off },
                        new SafetySetting { Category = HarmCategory.HarmCategoryHateSpeech, Threshold = HarmBlockThreshold.Off },
                        new SafetySetting { Category = HarmCategory.HarmCategorySexuallyExplicit, Threshold = HarmBlockThreshold.Off },
                        new SafetySetting { Category = HarmCategory.HarmCategoryDangerousContent, Threshold = HarmBlockThreshold.Off },
                    }
                };

                var response = await _moderationClient.Models.GenerateContentAsync(
                    model: _model,
                    contents: contents,
                    config: config
                );

                var text = response?.Candidates?.FirstOrDefault()?.Content?.Parts
                    ?.FirstOrDefault(p => !string.IsNullOrEmpty(p.Text))?.Text;

                if (string.IsNullOrEmpty(text))
                {
                    _logger.LogWarning("AI Moderation returned empty response for comment: {Comment}", comment);
                    return ModerationResult.Clean(); // Fail-open
                }

                var jsonResult = JsonSerializer.Deserialize<ModerationJsonResponse>(text, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (jsonResult == null)
                {
                    _logger.LogWarning("AI Moderation JSON parse returned null for: {Text}", text);
                    return ModerationResult.Clean();
                }

                return jsonResult.IsClean
                    ? ModerationResult.Clean()
                    : ModerationResult.Flagged(jsonResult.Reason ?? "Nội dung không phù hợp.");
            }
            catch (Exception ex)
            {
                // FAIL-OPEN: If AI fails, don't block the user
                _logger.LogError(ex, "AI Moderation failed for comment: {Comment}. Defaulting to clean.", comment);
                return ModerationResult.Clean();
            }
        }

        private class ModerationJsonResponse
        {
            public bool IsClean { get; set; }
            public string? Reason { get; set; }
        }
    }
}
