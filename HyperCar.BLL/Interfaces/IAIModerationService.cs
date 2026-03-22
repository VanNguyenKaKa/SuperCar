namespace HyperCar.BLL.Interfaces
{
    /// <summary>
    /// AI content moderation service — detects toxic/spam review content.
    /// Uses a SEPARATE Gemini API key from the chatbot.
    /// </summary>
    public interface IAIModerationService
    {
        /// <summary>
        /// Analyze review comment for profanity, harassment, or spam.
        /// Returns (isClean, reason). If API fails, defaults to isClean=true.
        /// </summary>
        Task<ModerationResult> AnalyzeReviewAsync(string comment);
    }

    public class ModerationResult
    {
        public bool IsClean { get; set; }
        public string Reason { get; set; } = string.Empty;

        public static ModerationResult Clean() => new() { IsClean = true, Reason = string.Empty };
        public static ModerationResult Flagged(string reason) => new() { IsClean = false, Reason = reason };
    }
}
