namespace HyperCar.BLL.DTOs
{
    public class ChatMessageDto
    {
        public string UserMessage { get; set; } = string.Empty;
        public string AiResponse { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class ChatRequestDto
    {
        public string Message { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? SessionId { get; set; }
    }
}
