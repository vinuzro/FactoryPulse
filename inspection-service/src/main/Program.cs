using FactoryPulse.Inspection.Data;
using FactoryPulse.Inspection.Hubs;
using FactoryPulse.Inspection.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// EF Core
builder.Services.AddDbContext<InspectionDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT auth — validation only (tokens issued by auth-service)
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

        // Let SignalR pass the token via query string
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

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("EngineerOrAdmin", p => p.RequireRole("ENGINEER", "ADMIN"));
    opts.AddPolicy("AdminOnly",       p => p.RequireRole("ADMIN"));
});

builder.Services.AddScoped<InspectionService>();
builder.Services.AddScoped<EquipmentService>();
builder.Services.AddScoped<AuditService>();

builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InspectionDbContext>();
    db.Database.Migrate();
}

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<StatusHub>("/hubs/status");

app.Run();
