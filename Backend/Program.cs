using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using API;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Backend.Interfaces.IRepositories;
using Backend.Repositories;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Backend.Interfaces;
using Backend.Services;
using Backend.Models;
using Backend.Hubs;
using System.Net.Http.Headers;
using Backend.DbContext;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


if (File.Exists(".env"))
{
    Env.Load();
}

builder.Services.AddHttpClient("openai", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    var apiKey = Environment.GetEnvironmentVariable("API_KEY");
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

builder.Services.AddHttpClient("iot", client =>
{
    var iotUrl = Environment.GetEnvironmentVariable("IOT_SERVICE_URL") ?? "http://localhost:5101/";
    client.BaseAddress = new Uri(iotUrl);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

builder.Services.AddHttpClient("rules", client =>
{
    var rulesUrl = Environment.GetEnvironmentVariable("RULES_SERVICE_URL") ?? "http://localhost:5105/";
    client.BaseAddress = new Uri(rulesUrl);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

builder.Services.AddOpenApi();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddScoped<IBookingActionService, BookingActionService>();
builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var envHost = Environment.GetEnvironmentVariable("DB_HOST");
    string cs;
    
    if (!string.IsNullOrEmpty(envHost))
    {
        var host = envHost;
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
        var user = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
        var pass = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";
        var db = Environment.GetEnvironmentVariable("DB_NAME") ?? "innoviahub";
        
        cs = $"Server={host};Port={port};Database={db};User={user};Password={pass};TreatTinyAsBoolean=true";
        
        Console.WriteLine($"🔧 Using Docker environment variables:");
        Console.WriteLine($"   Host: {host}");
        Console.WriteLine($"   Port: {port}");
        Console.WriteLine($"   Database: {db}");
        Console.WriteLine($"   User: {user}");
    }
    else
    {
        cs = builder.Configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Database connection string not found");
        Console.WriteLine("🔧 Using appsettings.json connection string");
    }

    // Add EnableRetryOnFailure for transient errors
    options.UseMySql(cs, new MySqlServerVersion(new Version(8, 0, 21)), 
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null));
});

// Enhanced CORS for production
var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(',') 
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddScoped<JwtToken>();

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("admin"));
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var key = builder.Configuration.GetValue<string>("Jwt:Key") 
        ?? Environment.GetEnvironmentVariable("JWT_KEY")
        ?? throw new InvalidOperationException("JWT Key not configured");
    var issuer = builder.Configuration.GetValue<string>("Jwt:Issuer") 
        ?? Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "innoviahub";
    var audience = builder.Configuration.GetValue<string>("Jwt:Audience") 
        ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "innoviahub";
    var keyBytes = Encoding.ASCII.GetBytes(key);

    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Database initialization with retry logic
Console.WriteLine("🔄 Initializing database...");
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Retry logic for database connection with longer delays
    int maxRetries = 15;
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            Console.WriteLine($"⏳ Attempting database connection... (Attempt {i + 1}/{maxRetries})");
            var canConnect = await db.Database.CanConnectAsync();
            if (canConnect)
            {
                Console.WriteLine("✅ Database connection successful!");
                // Apply any pending EF Core migrations before seeding
                try
                {
                    Console.WriteLine("🔧 Applying pending EF Core migrations (if any)...");
                    await db.Database.MigrateAsync();
                    Console.WriteLine("✅ EF Core migrations applied");
                }
                catch (Exception migEx)
                {
                    Console.WriteLine($"⚠️ Failed to apply migrations: {migEx.Message}");
                    // Continue - seeding may still fail, but we want to surface the migration error
                }

                break;
            }
        }
        catch (Exception ex)
        {
            if (i == maxRetries - 1)
            {
                Console.WriteLine($"❌ Failed to connect to database after {maxRetries} attempts");
                Console.WriteLine($"❌ Error: {ex.Message}");
                throw;
            }
            Console.WriteLine($"⚠️  Database not ready, waiting 5 seconds... ({ex.Message})");
            await Task.Delay(5000); // Wait 5 seconds between retries
        }
    }


    // Seed data
    try
    {
        Console.WriteLine("🔄 Seeding database...");
        await DbSeeder.Seed(db, userManager, roleManager);
        Console.WriteLine("✅ Database seeded successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Seeding failed: {ex.Message}");
        throw;
    }
}

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Serve static files from wwwroot (default ASP.NET location)
app.UseStaticFiles();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowReactApp");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map controllers and hubs
app.MapControllers();
app.MapHub<BookingHub>("/bookingHub");
app.MapHub<TtsHub>("/ttshub");

// Fallback to React app for client-side routing (SPA)
// This must be LAST - it catches all routes that aren't API endpoints
app.MapFallbackToFile("index.html");
Console.WriteLine("✅ React SPA fallback configured");

Console.WriteLine($"🚀 InnoviaHub API started");
Console.WriteLine($"📝 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"🌐 Access at: http://localhost:8080");

app.Run();