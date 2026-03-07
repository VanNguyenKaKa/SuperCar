using HyperCar.BLL.DTOs;

namespace HyperCar.BLL.Interfaces
{

    public interface IAIChatService
    {
        Task<ChatMessageDto> SendMessageAsync(ChatRequestDto request);
        Task<IEnumerable<ChatMessageDto>> GetHistoryAsync(string? userId, string? sessionId, int count = 20);
    }
}
