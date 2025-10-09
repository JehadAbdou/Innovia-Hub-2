using System.Text;
using System.Text.Json;
using Backend.Models.Chat;

namespace Backend.Services
{
    public class AiService : IAiService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AiService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> GetAiResponseAsync(
            List<ChatMessage> history,
            string systemMessage,
            string userMessage,
            string fallbackMessage)
        {
            var http = _httpClientFactory.CreateClient("openai");
            
            var aiPrompt = new List<object>
            {
                new { role = "system", content = systemMessage }
            };

            // Add conversation history for language context
            foreach (var msg in history)
            {
                aiPrompt.Add(new { role = msg.Role, content = msg.Content });
            }

            // Add the specific instruction
            aiPrompt.Add(new { role = "user", content = userMessage });

            var bodyForAi = new { model = "gpt-4.1", input = aiPrompt.ToArray() };
            var contentAi = new StringContent(JsonSerializer.Serialize(bodyForAi), Encoding.UTF8, "application/json");
            var responseAi = await http.PostAsync("responses", contentAi);
            var rawAi = await responseAi.Content.ReadAsStringAsync();

            try
            {
                var docAi = JsonDocument.Parse(rawAi);
                return docAi.RootElement
                    .GetProperty("output")[0]
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString() ?? fallbackMessage;
            }
            catch
            {
                return fallbackMessage;
            }
        }
    }
}