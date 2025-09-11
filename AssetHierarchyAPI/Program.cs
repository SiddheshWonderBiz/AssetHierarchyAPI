using AssetHierarchyAPI.Data;
using AssetHierarchyAPI.Extensions;
using AssetHierarchyAPI.Hubs;
using AssetHierarchyAPI.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Storage service extension
builder.Services.AddStorageService(builder.Configuration);

// Serilog UI
builder.Services.AddSerilogUi(_ => { });
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme; // ✅ important
})
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
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey("AuthToken"))
            {
                context.Token = context.Request.Cookies["AuthToken"];
            }
            else if (!string.IsNullOrEmpty(context.Request.Query["access_token"]) &&
                     context.HttpContext.Request.Path.StartsWithSegments("/notificationHub"))
            {
                context.Token = context.Request.Query["access_token"];
            }

            return Task.CompletedTask;
        },
    };
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme) // must exist
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    options.CallbackPath = "/google-callback";
})
.AddGitHub(options =>
{
    options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"]!;
    options.CallbackPath = "/github-callback";
    options.Scope.Add("user:email"); // to ensure email is returned
});


builder.Services.AddAuthorization();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

// Authentication must come before authorization
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogUi();
app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");

app.Run();