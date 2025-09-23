using AiQaMiniApi9.Models;
using AiQaMiniApi9.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Настройка HttpClient для Ollama
builder.Services.AddHttpClient<IOllamaClient, OllamaClient>(http =>
{
    var baseUrl = builder.Configuration["Ollama:BaseUrl"] ?? "http://127.0.0.1:11434";
    http.BaseAddress = new Uri(baseUrl);
    http.Timeout = TimeSpan.FromMinutes(2);
});

// Регистрация сервисов приложения
builder.Services.AddSingleton<ITranslationChecker, TranslationChecker>();
builder.Services.AddSingleton<ITranslator, Translator>();

// Настройка API Explorer и Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Настройка CORS (разрешаем все для простоты)
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Настройка Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429; // Too Many Requests

    // Лимит для бесплатных запросов по IP (3 запроса в минуту)
    options.AddPolicy("free-by-ip", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetTokenBucketLimiter(ip, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 3,                 // Максимум 3 токена
            TokensPerPeriod = 3,            // Пополнение 3 токена в минуту
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            AutoReplenishment = true,       // Автоматическое пополнение
            QueueLimit = 0                  // Без очереди
        });
    });

    // Лимит для платных запросов по API-ключу (60 запросов в минуту)
    options.AddPolicy("per-api-key", httpContext =>
    {
        var key = httpContext.Items.TryGetValue("api_key", out var v) ? v?.ToString() ?? "anon" : "anon";
        return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 60,                // Высокий лимит для платных пользователей
            TokensPerPeriod = 60,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            AutoReplenishment = true,
            QueueLimit = 0
        });
    });
});

// Дополнительные сервисы
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IApiKeyStore, InMemoryApiKeyStore>();

// Расширенная настройка Swagger для API-ключей
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Translator API", Version = "v1" });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Введите API-ключ в поле: X-API-Key",
        Type = SecuritySchemeType.ApiKey,
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Scheme = "ApiKeyScheme"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Middleware pipeline
app.UseCors();
app.UseRateLimiter(); // Включаем Rate Limiter

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health check endpoint
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

// БЕСПЛАТНЫЕ эндпоинты (с лимитом по IP)
app.MapPost("/api/translate/check",
    async ([FromBody] TranslationCheckRequest req, ITranslationChecker checker, CancellationToken ct) =>
    {
        // Валидация входных данных
        if (string.IsNullOrWhiteSpace(req.SourceLanguage) ||
            string.IsNullOrWhiteSpace(req.SourceText) ||
            string.IsNullOrWhiteSpace(req.TargetLanguage) ||
            string.IsNullOrWhiteSpace(req.TargetText))
            return Results.BadRequest(new { error = "sourceLanguage, sourceText, targetLanguage, targetText are required" });

        // Проверка размера текста
        if ((req.SourceText.Length + req.TargetText.Length) > 100_000)
            return Results.BadRequest(new { error = "payload too large" });

        var result = await checker.CheckAsync(req, ct);
        return Results.Ok(result);
    })
    .RequireRateLimiting("free-by-ip") // Применяем лимит по IP
    .DisableAntiforgery()
    .WithName("CheckTranslation")
    .Produces<TranslationCheckResult>(200)
    .Produces(400);

app.MapPost("/api/translate",
    async ([FromBody] TranslationRequest req, ITranslator translator, CancellationToken ct) =>
    {
        // Валидация входных данных
        if (string.IsNullOrWhiteSpace(req.SourceLanguage) ||
            string.IsNullOrWhiteSpace(req.SourceText) ||
            string.IsNullOrWhiteSpace(req.TargetLanguage))
            return Results.BadRequest(new { error = "sourceLanguage, sourceText, targetLanguage are required" });

        // Проверка размера текста
        if (req.SourceText.Length > 100_000)
            return Results.BadRequest(new { error = "payload too large" });

        var result = await translator.TranslateAsync(req, ct);
        return Results.Ok(result);
    })
    .RequireRateLimiting("free-by-ip") // Применяем лимит по IP
    .DisableAntiforgery()
    .WithName("Translation")
    .Produces<TranslationResult>(200)
    .Produces(400);

// ПЛАТНЫЕ эндпоинты (с лимитом по API-ключу)
var paid = app.MapGroup("/api/v1")
    .RequireRateLimiting("per-api-key"); // Групповой лимит по API-ключу

paid.MapPost("/translate", async (
        [FromBody] TranslationRequest req,
        [FromHeader(Name = "X-API-Key")] string? xApiKey,
        ITranslator translator,
        IApiKeyStore store,
        ILogger<Program> logger,
        CancellationToken ct) =>
{
    // Получаем API-ключ из заголовка или контекста
    string apiKey = xApiKey
        ?? (req.Context != null && req.Context.TryGetValue("apiKey", out var k) ? k : null)
        ?? "";

    // Проверка валидности API-ключа
    if (string.IsNullOrWhiteSpace(apiKey) || !store.IsValid(apiKey))
        return Results.Unauthorized();

    // Проверка и списание квоты символов
    if (!await store.TryConsumeAsync(apiKey, req.SourceText?.Length ?? 0, ct))
        return Results.StatusCode(402); // Payment Required

    // Выполнение перевода
    var result = await translator.TranslateAsync(req, ct);

    // Логирование успешного запроса
    logger.LogInformation("PAID OK key={Key} chars={Chars}",
        apiKey, req.SourceText?.Length ?? 0);

    return Results.Ok(result);
})
    .WithName("PaidTranslation")
    .DisableAntiforgery();

// Перенаправление на Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();