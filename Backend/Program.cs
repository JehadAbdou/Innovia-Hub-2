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

Env.Load();
builder.Services.AddHttpClient("openai", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    var apiKey = Environment.GetEnvironmentVariable("API-KEY");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddHttpClient("iot", client =>
{
    client.BaseAddress = new Uri("http://localhost:5101/");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddHttpClient("rules", client =>
{
    client.BaseAddress = new Uri("http://localhost:5105/");
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
        var user = Environment.GetEnvironmentVariable("DB_USER") ?? "";
        var pass = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";
        var db = Environment.GetEnvironmentVariable("DB_NAME") ?? "";
        cs = $"Server={host};Port={port};Database={db};User={user};Password={pass};TreatTinyAsBoolean=true";
    }
    else
    {
        cs = builder.Configuration.GetConnectionString("DefaultConnection");
    }

    options.UseMySql(cs, ServerVersion.AutoDetect(cs));
});
builder.Services.AddCors(options =>
    {
       options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173") 
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
    var key = builder.Configuration.GetValue<string>("Jwt:Key");
    var issuer = builder.Configuration.GetValue<string>("Jwt:Issuer");
    var audience = builder.Configuration.GetValue<string>("Jwt:Audience");
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
// Seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    
    DbSeeder.Seed(db, userManager, roleManager).Wait();
}
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "dist")),
    RequestPath = ""
});

// Make React handle client-side routing
app.MapFallbackToFile("index.html", new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "dist"))
});

app.UseCors("AllowReactApp");
app.UseRouting();
app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.MapHub<BookingHub>("/bookingHub");
app.MapHub<TtsHub>("/ttshub");

app.Run();