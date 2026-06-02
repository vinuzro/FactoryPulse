using FactoryPulse.Reporting.Data;
using FactoryPulse.Reporting.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ReportingDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
        };
    });

builder.Services.AddAuthorization();

// HttpClient for pulling inspection data
builder.Services.AddHttpClient<InspectionDataClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["InspectionService:BaseUrl"] ?? "http://localhost:5001");
});

builder.Services.AddScoped<PdfReportService>();
builder.Services.AddScoped<ExcelReportService>();
builder.Services.AddScoped<SummaryService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
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

app.Run();
