using AiQaMiniApi9.Models;
using AiQaMiniApi9.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<IOllamaClient, OllamaClient>(http =>
{
    var baseUrl = builder.Configuration["Ollama:BaseUrl"] ?? "http://127.0.0.1:11434";
    http.BaseAddress = new Uri(baseUrl);
    http.Timeout = TimeSpan.FromMinutes(2);
});
builder.Services.AddSingleton<ITranslationChecker, TranslationChecker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/translate/check",
    async ([FromBody] TranslationCheckRequest req, ITranslationChecker checker, CancellationToken ct) => // <== [FromBody]
    {
        if (string.IsNullOrWhiteSpace(req.SourceLanguage) ||
            string.IsNullOrWhiteSpace(req.SourceText) ||
            string.IsNullOrWhiteSpace(req.TargetLanguage) ||
            string.IsNullOrWhiteSpace(req.TargetText))
            return Results.BadRequest(new { error = "sourceLanguage, sourceText, targetLanguage, targetText are required" });

        if ((req.SourceText.Length + req.TargetText.Length) > 100_000)
            return Results.BadRequest(new { error = "payload too large" });

        var result = await checker.CheckAsync(req, ct);
        return Results.Ok(result);
    })
    .DisableAntiforgery()            // JSON-API из другого сервера → антифорджери мешает
    .WithName("CheckTranslation")
    .Produces<TranslationCheckResult>(200)
    .Produces(400);

app.MapGet("/", () => Results.Redirect("/swagger"));
app.Run();
