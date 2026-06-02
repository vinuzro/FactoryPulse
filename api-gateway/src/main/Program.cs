using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT secret not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer           = false,
            ValidateAudience         = false,
            ClockSkew                = TimeSpan.FromSeconds(30)
        };

        // Allow SignalR token via query string
        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// All routes except /api/auth/login and /api/auth/validate require a valid JWT
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";

    bool isPublic = path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase)
                 || path.StartsWith("/api/auth/validate", StringComparison.OrdinalIgnoreCase)
                 || path.StartsWith("/hubs/", StringComparison.OrdinalIgnoreCase); // SignalR handles its own auth

    if (!isPublic && !(context.User.Identity?.IsAuthenticated ?? false))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
        return;
    }

    await next();
});

app.MapReverseProxy();

app.Run();
