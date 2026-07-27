namespace ShoppingList.Api.Security;

public static class ProductionSecurityValidator
{
    public static void Validate(IHostEnvironment environment, IConfiguration configuration, ILogger logger)
    {
        if (environment.IsDevelopment())
        {
            return;
        }

        if (configuration.GetValue("Swagger:Enabled", false))
        {
            throw new InvalidOperationException(
                "Swagger:Enabled must be false outside Development.");
        }

        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length == 0)
        {
            throw new InvalidOperationException(
                "Cors:AllowedOrigins must contain at least one production frontend origin.");
        }

        foreach (var origin in origins)
        {
            if (origin.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                || origin.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || origin.Contains("your-domain", StringComparison.OrdinalIgnoreCase)
                || origin.Contains("REPLACE_", StringComparison.OrdinalIgnoreCase)
                || origin.Contains("example.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Cors:AllowedOrigins contains a non-production value: '{origin}'.");
            }

            if (!origin.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Cors:AllowedOrigins entries must use https in production: '{origin}'.");
            }
        }

        var frontendBase = configuration["App:FrontendBaseUrl"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(frontendBase)
            || frontendBase.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || frontendBase.Contains("your-domain", StringComparison.OrdinalIgnoreCase)
            || frontendBase.Contains("REPLACE_", StringComparison.OrdinalIgnoreCase)
            || frontendBase.Contains("example.com", StringComparison.OrdinalIgnoreCase)
            || !frontendBase.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "App:FrontendBaseUrl must be a https production URL.");
        }

        var allowedHosts = configuration["AllowedHosts"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(allowedHosts)
            || allowedHosts.Contains("your-domain", StringComparison.OrdinalIgnoreCase)
            || allowedHosts.Contains("REPLACE_", StringComparison.OrdinalIgnoreCase)
            || allowedHosts.Contains("example.com", StringComparison.OrdinalIgnoreCase)
            || string.Equals(allowedHosts, "*", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "AllowedHosts must be set to your real host name(s); do not use placeholders or '*'.");
        }

        var proxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
        var networks = configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
        if (proxies.Length == 0 && networks.Length == 0)
        {
            logger.LogWarning(
                "ForwardedHeaders:KnownProxies/KnownNetworks are empty. " +
                "X-Forwarded-For will be ignored. If you terminate TLS at a reverse proxy/load balancer, " +
                "configure KnownProxies (or KnownNetworks) so client IP rate limiting works correctly.");
        }
        else
        {
            logger.LogInformation(
                "Forwarded headers trust configured with {ProxyCount} proxies and {NetworkCount} networks.",
                proxies.Length,
                networks.Length);
        }
    }
}
