using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Text;

public class TtsHub : Hub
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TtsHub(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendTextToTts(string text)
    {
        var client = _httpClientFactory.CreateClient("openai");

        var body = new
        {
            model = "gpt-4o-mini-tts",
            input = text,
            voice = "alloy"
        };

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("audio/speech?stream=true", content);

        response.EnsureSuccessStatusCode();

        // Stream the audio in chunks
        using var stream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[4096];
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
        {
            var chunk = Convert.ToBase64String(buffer, 0, bytesRead);
            await Clients.Caller.SendAsync("audioChunk", chunk);
        }

        await Clients.Caller.SendAsync("audioDone");
    }
}
