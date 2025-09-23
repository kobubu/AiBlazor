namespace AiQaMiniApi9.Services;

public interface IApiKeyStore
{
    bool IsValid(string key);
    Task<bool> TryConsumeAsync(string key, int chars, CancellationToken ct);
}

public sealed class InMemoryApiKeyStore(IConfiguration cfg) : IApiKeyStore
{
    private readonly HashSet<string> _keys =
        cfg.GetSection("ApiKeys:Paid").Get<string[]>()?.ToHashSet() ?? [];

    // простая дневная квота по ключу
    private readonly Dictionary<(string key, DateOnly day), int> _usage = new();

    private const int DailyLimitChars = 200_000; // пример: 200k символов/день

    public bool IsValid(string key) => _keys.Contains(key);

    public Task<bool> TryConsumeAsync(string key, int chars, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var k = (key, today);
        _usage.TryGetValue(k, out var used);
        if (used + chars > DailyLimitChars) return Task.FromResult(false);
        _usage[k] = used + chars;
        return Task.FromResult(true);
    }
}
