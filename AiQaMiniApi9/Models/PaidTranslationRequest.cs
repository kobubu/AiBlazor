namespace AiQaMiniApi9.Models
{
    public sealed class PaidTranslationRequest
    {
        public string SourceLanguage { get; set; } = "";
        public string SourceText { get; set; } = "";
        public string TargetLanguage { get; set; } = "";

    }
}
