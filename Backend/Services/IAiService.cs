using Backend.Models.Chat;

namespace Backend.Services
{
    public interface IAiService
    {
        Task<string> GetAiResponseAsync(
            List<ChatMessage> history,
            string systemMessage,
            string userMessage,
            string fallbackMessage);
    }
}