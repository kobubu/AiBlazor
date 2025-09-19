using System.Net.Http.Json;

namespace AiQaTranslator9Auth.Services;

public interface ITranslationApi
{
    Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken = default);
}

public sealed class TranslationApi(HttpClient http) : ITranslationApi
{
    private readonly HttpClient _http = http;

    //TODO: make an "/api/translate" endpoint in AiQaMiniApi9
    public async Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/translate/", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"API error {(int)response.StatusCode}: {errorText}");
        }
        var result = await response.Content.ReadFromJsonAsync<TranslationResult>(cancellationToken: cancellationToken);
        return result ?? new TranslationResult { IsOk = false, Translation = "Something went wrong..."};
    }
}

public sealed class TranslationRequest
{
    public string SourceLanguage { get; set; } = string.Empty;
    public string SourceText { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = string.Empty;
    public Dictionary<string, string>? Context { get; set; }
}

public sealed class TranslationResult
{
    public bool IsOk { get; set; }
    public string Translation { get; set; } = string.Empty;
}


