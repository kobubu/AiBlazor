using System.Text;
using AiQaMiniApi9.Models;

namespace AiQaMiniApi9.Services;

public interface ITranslator
{
    Task<TranslationResult> TranslateAsync(TranslationRequest req, CancellationToken ct = default);
}

public sealed class Translator(IOllamaClient llm, IConfiguration cfg) : ITranslator
{
    //todo: поменять на Химеру
    private readonly string _model = cfg["Ollama:TranslationModel"] ?? "hf.co/NikolayKozloff/Hunyuan-MT-Chimera-7B-Q8_0-GGUF:latest";
    private readonly float _temp = float.TryParse(cfg["Ollama:Temperature"], out var t) ? t : 0.25f;

    public async Task<TranslationResult> TranslateAsync(TranslationRequest r, CancellationToken ct = default)
    {
        var prompt = BuildPrompt(r);
        var raw = await llm.GenerateAsync(prompt, _model, _temp, ct);
        return new TranslationResult { IsOk = true, Translation = raw };
    }

    private static string BuildPrompt(TranslationRequest r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a professional translator.");
        sb.AppendLine("Translate the following Source text from Source language to Target language. Don't provide any comments. I need translation only");
        sb.AppendLine();
        sb.AppendLine($"Source language: {r.SourceLanguage}");
        sb.AppendLine($"Source text: {r.SourceText}");
        sb.AppendLine();
        sb.AppendLine($"Target language: {r.TargetLanguage}");
        sb.AppendLine();
        
        return sb.ToString();
    }
}
