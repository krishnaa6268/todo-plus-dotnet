using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TodoPlus.Data;
using TodoPlus.Models;
using TodoPlus.Services;

// Load environment variables from .env file if it exists
if (File.Exists(".env"))
{
    DotNetEnv.Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure MongoDB Settings & Context
builder.Services.Configure<MongoDbSettings>(options =>
{
    builder.Configuration.GetSection("MongoDbSettings").Bind(options);

    var connectionString = Environment.GetEnvironmentVariable("MONGODB_URI") 
                        ?? Environment.GetEnvironmentVariable("MONGO_URL") 
                        ?? Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
                        ?? Environment.GetEnvironmentVariable("MongoDbSettings__ConnectionString");

    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.ConnectionString = connectionString;
    }

    var databaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE") 
                    ?? Environment.GetEnvironmentVariable("MongoDbSettings__DatabaseName");

    if (!string.IsNullOrWhiteSpace(databaseName))
    {
        options.DatabaseName = databaseName;
    }

    var collectionName = Environment.GetEnvironmentVariable("MONGODB_COLLECTION") 
                      ?? Environment.GetEnvironmentVariable("MongoDbSettings__CollectionName");

    if (!string.IsNullOrWhiteSpace(collectionName))
    {
        options.CollectionName = collectionName;
    }
});

builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<IJwtService, JwtService>();

// Configure JWT Authentication
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["JwtSettings:SecretKey"]
    ?? "SuperSecretTodoPlusJwtSigningKey2026WithAtLeast256BitsOfEntropy!!";

var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? builder.Configuration["JwtSettings:Issuer"] 
    ?? "TodoPlusApp";

var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? builder.Configuration["JwtSettings:Audience"] 
    ?? "TodoPlusUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5)
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            string? token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (string.IsNullOrEmpty(token) && context.Request.Cookies.ContainsKey("JwtToken"))
            {
                token = context.Request.Cookies["JwtToken"];
            }

            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            if (!context.Response.HasStarted && !context.Request.Path.StartsWithSegments("/api"))
            {
                context.HandleResponse();
                var returnUrl = context.Request.Path + context.Request.QueryString;
                context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            }
            return Task.CompletedTask;
        },
        OnForbidden = context =>
        {
            if (!context.Response.HasStarted && !context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.Redirect("/Account/AccessDenied");
            }
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

// Ensure MongoDB collection has seed data on startup
using (var scope = app.Services.CreateScope())
{
    var mongoContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    await mongoContext.SeedDataAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Todo}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
