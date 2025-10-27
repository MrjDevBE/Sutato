using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Sutato.Server.Features.Auth;
using Sutato.Server.Features.Dashboard;
using Sutato.Shared.Features.Auth;
using Sutato.Shared.Features.Common.Constants;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile(Path.Combine("Config", "appsettings.json"), optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Bind Config Sections
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));

// Register Services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddControllers();

// Read allowed origins from config
var allowedOrigins = builder.Configuration
    .GetSection("ApiSettings:HttpsClient1")
    .Get<string[]>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// JWT
var secret = builder.Configuration["JwtSettings:Secret"] ?? throw new InvalidOperationException("Missing JwtSettings:Secret");
var issuer = builder.Configuration["JwtSettings:Issuer"] ?? throw new InvalidOperationException("Missing JwtSettings:Issuer");
var audience = builder.Configuration["JwtSettings:Audience"] ?? throw new InvalidOperationException("Missing JwtSettings:Audience");

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                    context.Response.Headers.Add("Token-Expired", "true");

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();

// -------------------
// Middleware Pipeline
// -------------------
var app = builder.Build();

app.UseCors("AllowBlazorClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<DashboardHub>("/hubs/dashboard");

// 🟢 Background Dashboard Demo Updates (safe start)
_ = Task.Run(async () =>
{
    await Task.Delay(2000); // small delay to let app fully boot
    using var scope = app.Services.CreateScope();
    var dashboardService = scope.ServiceProvider.GetRequiredService<DashboardService>();

    try
    {
        await dashboardService.StartDemoUpdates();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DashboardService] Error in background updates: {ex.Message}");
    }
});

app.Run();
