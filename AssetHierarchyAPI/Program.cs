using AssetHierarchyAPI.Data;
using AssetHierarchyAPI.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Ui.Web;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Connect")));

// CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Storage service extension
builder.Services.AddStorageService(builder.Configuration);

// Serilog UI
builder.Services.AddSerilogUi(_ => { });

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            ),
            RoleClaimType = ClaimTypes.Role, // ensures [Authorize(Roles="")] works
            NameClaimType = ClaimTypes.Name
        };
    });

builder.Services.AddAuthorization();


builder.Services.AddAuthorization();

var app = builder.Build();

// ✅ CRITICAL: Middleware pipeline order matters!
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

// Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogUi();
app.MapControllers();

Console.WriteLine("🚀 Application started with JWT authentication configured");
Console.WriteLine($"JWT Issuer: {builder.Configuration["Jwt:Issuer"]}");
Console.WriteLine($"JWT Audience: {builder.Configuration["Jwt:Audience"]}");
Console.WriteLine($"JWT Key Length: {builder.Configuration["Jwt:Key"]?.Length}");

app.Run();