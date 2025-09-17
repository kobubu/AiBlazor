namespace AiQaMiniApi9.Models;

public sealed class TranslationCheckResult
{
    public bool IsOk { get; set; }
    public int TotalErrors { get; set; }
    public string RawAnalysis { get; set; } = "";
}
