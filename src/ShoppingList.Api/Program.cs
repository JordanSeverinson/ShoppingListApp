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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Cook In Shop Out API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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
});

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
builder.Services.AddSingleton<JwtDenylistService>();

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
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

                if (!string.IsNullOrWhiteSpace(rawToken) && denylist.IsRevoked(rawToken))
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

builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .WithHeaders("Content-Type", "Authorization")
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
            .AllowCredentials());
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});

app.UseExceptionHandler();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

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
