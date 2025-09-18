using System.Text;
using AiQaMiniApi9.Models;

namespace AiQaMiniApi9.Services;

public interface ITranslationChecker
{
    Task<TranslationCheckResult> CheckAsync(TranslationCheckRequest req, CancellationToken ct = default);
}

public sealed class TranslationChecker(IOllamaClient llm, IConfiguration cfg) : ITranslationChecker
{
    private readonly string _model = cfg["Ollama:Model"] ?? "qwen3:32b";
    private readonly float _temp = float.TryParse(cfg["Ollama:Temperature"], out var t) ? t : 0.25f;

    public async Task<TranslationCheckResult> CheckAsync(TranslationCheckRequest r, CancellationToken ct = default)
    {
        var prompt = BuildPrompt(r);
        var raw = await llm.GenerateAsync(prompt, _model, _temp, ct);
        var (ok, total) = Parse(raw);
        return new TranslationCheckResult { IsOk = ok, TotalErrors = total, RawAnalysis = raw };
    }

    private static string BuildPrompt(TranslationCheckRequest r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a professional translation reviewer that ignores minor or preferential issues.");
        sb.AppendLine("Rules:");
        sb.AppendLine("1) Evaluate if target matches source semantics given the context.");
        sb.AppendLine("2) Names/skills/locations and format are always correct; ignore formatting issues.");
        sb.AppendLine("3) If translation is OK: [OK] <lang>: Translation is correct.");
        sb.AppendLine("4) If there is a major error: [ERROR] <lang>: <short description>.");
        sb.AppendLine("5) Finish with: TOTAL_ERRORS: X");
        sb.AppendLine();
        sb.AppendLine($"Source language: {r.SourceLanguage}");
        sb.AppendLine($"Source text: {r.SourceText}");
        sb.AppendLine();
        sb.AppendLine($"Target language: {r.TargetLanguage}");
        sb.AppendLine($"Target text: {r.TargetText}");
        sb.AppendLine();
        sb.AppendLine("Context:");
        if (r.Context is { Count: > 0 })
            foreach (var kv in r.Context) sb.AppendLine($"{kv.Key}: {kv.Value}");
        else
            sb.AppendLine("No additional context provided");
        sb.AppendLine();
        sb.AppendLine("/no_think");
        return sb.ToString();
    }

    private static (bool ok, int totalErrors) Parse(string raw)
    {
        int total = 0;
        foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("TOTAL_ERRORS:", StringComparison.OrdinalIgnoreCase))
            {
                var tail = line.Split(':', 2)[1].Trim();
                var num = new string(tail.TakeWhile(char.IsDigit).ToArray());
                if (int.TryParse(num, out var n)) total = n;
            }
        }
        if (total == 0)
            total = raw.Split('\n').Count(l => l.Contains("[ERROR]", StringComparison.OrdinalIgnoreCase));
        return (total == 0, total);
    }
}
