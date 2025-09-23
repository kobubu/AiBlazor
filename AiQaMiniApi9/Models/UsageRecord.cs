namespace AiQaMiniApi9.Models
{
    public sealed class UsageRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ApiKey { get; set; } = "";
        public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
        public int Chars { get; set; }
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public long LatencyMs { get; set; }
    }
}
