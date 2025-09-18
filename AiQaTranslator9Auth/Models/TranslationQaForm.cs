using System.ComponentModel.DataAnnotations;

namespace AiQaTranslator9Auth.Models;

public sealed class TranslationQaForm
{
    [Required, MinLength(1)]
    public string SourceLanguage { get; set; } = "en";

    [Required, MinLength(1)]
    public string SourceText { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string TargetLanguage { get; set; } = "ru";

    [Required, MinLength(1)]
    public string TargetText { get; set; } = string.Empty;
}


