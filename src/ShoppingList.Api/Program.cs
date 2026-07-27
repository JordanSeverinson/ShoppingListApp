using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ShoppingList.Application.Recipes;
using ShoppingList.Api.Hubs;
using ShoppingList.Api.Persistence;
using ShoppingList.Api.Security;
using ShoppingList.Api.Services;
using ShoppingList.Infrastructure;
using ShoppingList.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true,
    reloadOnChange: true);

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new RecipeContentRootJsonConverter());
    });

// Swagger is Development-only and must be explicitly enabled (never available in Staging/Production).
var swaggerEnabled = builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue("Swagger:Enabled", false);

if (swaggerEnabled)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Cook In Shop Out API", Version = "v1" });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description =
                "Optional. Paste a JWT as Bearer. Prefer logging in via /api/auth/login so the auth_token cookie is set.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        options.AddSecurityDefinition("Cookie", new OpenApiSecurityScheme
        {
            Description = $"HttpOnly JWT cookie ({AuthConstants.CookieName}) set by login/register.",
            Name = AuthConstants.CookieName,
            In = ParameterLocation.Cookie,
            Type = SecuritySchemeType.ApiKey
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Cookie" }
                },
                Array.Empty<string>()
            }
        });
    });
}

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton<OcrProcessingGate>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("RateLimiter");
        logger.LogWarning(
            "Rate limit exceeded for {Method} {Path} from {RemoteIp}",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path,
            context.HttpContext.Connection.RemoteIpAddress);

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();
        }

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Too many requests. Please try again later." },
            cancellationToken);
    };

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            ClientIpResolver.GetClientIp(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    options.AddPolicy("friend-lookup", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            ClientIpResolver.GetClientIp(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    options.AddPolicy("ocr", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            ClientIpResolver.GetClientIp(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuthCookieService>();
builder.Services.AddScoped<CsrfTokenService>();
builder.Services.AddScoped<ListAccessService>();
builder.Services.AddScoped<RecipeAccessService>();
builder.Services.AddScoped<RecipeSharingService>();
builder.Services.AddScoped<ListSharingService>();
builder.Services.AddScoped<FriendCodeAllocationService>();
builder.Services.AddScoped<FriendsService>();
builder.Services.AddScoped<EmailVerificationService>();
builder.Services.AddScoped<PasswordResetService>();
builder.Services.AddScoped<UserSecurityStampService>();
builder.Services.AddSingleton<HubConnectionTracker>();
builder.Services.AddScoped<ListHubNotifier>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<JwtDenylistService>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IEmailSender, DevelopmentEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
}

builder.Services.AddSignalR();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
if (Encoding.UTF8.GetByteCount(jwtKey) < AuthConstants.MinJwtKeyBytes)
{
    throw new InvalidOperationException(
        $"Jwt:Key must be at least {AuthConstants.MinJwtKeyBytes} bytes when UTF-8 encoded.");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Do not trust forwarded headers unless proxies/networks are explicitly configured.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();

    foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
    {
        if (System.Net.IPAddress.TryParse(proxy, out var address))
        {
            options.KnownProxies.Add(address);
        }
    }

    foreach (var network in builder.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [])
    {
        var parts = network.Split('/', 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2
            && System.Net.IPAddress.TryParse(parts[0], out var prefix)
            && int.TryParse(parts[1], out var prefixLength))
        {
            options.KnownIPNetworks.Add(new System.Net.IPNetwork(prefix, prefixLength));
        }
    }
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero,
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (!string.IsNullOrEmpty(context.Token))
                {
                    return Task.CompletedTask;
                }

                if (context.Request.Cookies.TryGetValue(AuthConstants.CookieName, out var cookieToken)
                    && !string.IsNullOrWhiteSpace(cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var denylist = context.HttpContext.RequestServices.GetRequiredService<JwtDenylistService>();
                var rawToken = context.HttpContext.Request.Cookies[AuthConstants.CookieName];
                if (string.IsNullOrWhiteSpace(rawToken))
                {
                    var authHeader = context.HttpContext.Request.Headers.Authorization.ToString();
                    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        rawToken = authHeader["Bearer ".Length..].Trim();
                    }
                }

                if (!string.IsNullOrWhiteSpace(rawToken)
                    && await denylist.IsRevokedAsync(rawToken, context.HttpContext.RequestAborted))
                {
                    context.Fail("Token has been revoked.");
                    return;
                }

                var userIdValue = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var stampValue = context.Principal?.FindFirst(AuthConstants.SecurityStampClaimType)?.Value;
                if (!Guid.TryParse(userIdValue, out var userId) || !Guid.TryParse(stampValue, out var tokenStamp))
                {
                    context.Fail("Invalid token.");
                    return;
                }

                var stampService = context.HttpContext.RequestServices
                    .GetRequiredService<UserSecurityStampService>();
                var currentStamp = await stampService.GetStampAsync(
                    userId,
                    context.HttpContext.RequestAborted);

                if (currentStamp == Guid.Empty || currentStamp != tokenStamp)
                {
                    context.Fail("Token has been revoked.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
            // Origins are allowlisted; any header is fine for SPA + SignalR negotiate.
            .AllowAnyHeader()
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
            .AllowCredentials());
});

var app = builder.Build();

ProductionSecurityValidator.Validate(
    app.Environment,
    app.Configuration,
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ProductionSecurity"));

app.UseForwardedHeaders();

app.UseExceptionHandler();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<OriginAllowlistMiddleware>();
app.UseMiddleware<CookieCsrfMiddleware>();

if (!swaggerEnabled)
{
    // Defense in depth: never serve OpenAPI/Swagger outside the Development gate.
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await next();
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (swaggerEnabled)
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "swagger/{documentName}/swagger.json";
    });
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Cook In Shop Out API v1");
        options.RoutePrefix = "swagger";
        options.DisplayRequestDuration();
        options.EnablePersistAuthorization();
        options.UseRequestInterceptor(
            """
            (request) => {
              const match = document.cookie.match(/(?:^|; )csrf_token=([^;]*)/);
              if (match) {
                request.headers['X-CSRF'] = decodeURIComponent(match[1]);
              }
              return request;
            }
            """);
    });
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseInitializer");
    await DatabaseInitializer.MigrateAsync(db, logger);
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ShoppingListHub>("/hubs/shopping-list");

app.Run();
