using System.Net.Http.Json;

namespace AiQaTranslator9Auth.Services;

public interface ITranslationQaApi
{
    Task<TranslationCheckResult> CheckAsync(TranslationCheckRequest request, CancellationToken cancellationToken = default);
}

public sealed class TranslationQaApi(HttpClient http) : ITranslationQaApi
{
    private readonly HttpClient _http = http;

    public async Task<TranslationCheckResult> CheckAsync(TranslationCheckRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/translate/check", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"API error {(int)response.StatusCode}: {errorText}");
        }
        var result = await response.Content.ReadFromJsonAsync<TranslationCheckResult>(cancellationToken: cancellationToken);
        return result ?? new TranslationCheckResult { IsOk = false, RawAnalysis = "Empty response", TotalErrors = -1 };
    }
}

public sealed class TranslationCheckRequest
{
    public string SourceLanguage { get; set; } = string.Empty;
    public string SourceText { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = string.Empty;
    public string TargetText { get; set; } = string.Empty;
    public Dictionary<string, string>? Context { get; set; }
}

public sealed class TranslationCheckResult
{
    public bool IsOk { get; set; }
    public int TotalErrors { get; set; }
    public string RawAnalysis { get; set; } = string.Empty;
}


