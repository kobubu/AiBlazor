using System.Text.Json.Serialization;

public sealed class TranslationCheckRequest
{
    [JsonPropertyName("sourceLanguage")]
    public string? SourceLanguage { get; set; }

    [JsonPropertyName("sourceText")]
    public string? SourceText { get; set; }

    [JsonPropertyName("targetLanguage")]
    public string? TargetLanguage { get; set; }

    [JsonPropertyName("targetText")]
    public string? TargetText { get; set; }

    public Dictionary<string, string>? Context { get; set; }
}
