using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AiQaMiniApi9.Services;

public sealed class OllamaClient(HttpClient http) : IOllamaClient
{
    private sealed class OllamaRequest
    {
        // qwen3:32b, Inlingo1:latest
        [JsonPropertyName("model")] public string Model { get; set; } = "Inlingo1:latest";
        [JsonPropertyName("prompt")] public string Prompt { get; set; } = "";
        [JsonPropertyName("stream")] public bool Stream { get; set; } = false;
        [JsonPropertyName("temperature")] public float Temperature { get; set; } = 0.25f;
    }

    private sealed class OllamaResponse
    {
        [JsonPropertyName("response")] public string? Response { get; set; }
        [JsonPropertyName("done")] public bool Done { get; set; }
    }

    public async Task<string> GenerateAsync(string prompt, string model, float temperature, CancellationToken ct = default)
    {
        var req = new OllamaRequest { Model = model, Prompt = prompt, Stream = false, Temperature = temperature };
        var resp = await http.PostAsJsonAsync("/api/generate", req, ct);
        resp.EnsureSuccessStatusCode();
        var payload = await resp.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: ct);
        return (payload?.Response ?? "").Replace("<think>", "").Replace("</think>", "").Trim();
    }
}
