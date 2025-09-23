namespace AiQaMiniApi9.Services;

public static class PaidApiExtensions
{
    public static WebApplication UsePaidApiKey(this WebApplication app)
    {
        app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/api/v1"), branch =>
        {
            branch.Use(async (ctx, next) =>
            {
                var store = ctx.RequestServices.GetRequiredService<IApiKeyStore>();
                string? key = null;

                if (ctx.Request.Headers.TryGetValue("X-API-Key", out var hv)) key = hv.ToString();
                else if (ctx.Request.Headers.TryGetValue("Authorization", out var auth) &&
                         auth.ToString().StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
                    key = auth.ToString()["ApiKey ".Length..].Trim();

                if (string.IsNullOrWhiteSpace(key) || !store.IsValid(key))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    ctx.Response.Headers.WWWAuthenticate = "ApiKey";
                    await ctx.Response.WriteAsJsonAsync(new { error = "missing_or_invalid_api_key" });
                    return;
                }

                ctx.Items["api_key"] = key;
                await next();
            });
        });

        return app;
    }
}

