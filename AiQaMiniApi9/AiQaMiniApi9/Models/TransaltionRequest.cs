namespace AiQaMiniApi9.Models;

public sealed class TranslationCheckRequest
{
    public string SourceLanguage { get; set; } = "";
    public string SourceText { get; set; } = "";
    public string TargetLanguage { get; set; } = "";
    public string TargetText { get; set; } = "";
    public Dictionary<string, string>? Context { get; set; }
}