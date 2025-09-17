namespace AiQaMiniApi9.Services;

public interface IOllamaClient
{
    Task<string> GenerateAsync(string prompt, string model, float temperature, CancellationToken ct = default);
}
