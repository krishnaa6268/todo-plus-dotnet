using TodoPlus.Data;
using TodoPlus.Models;

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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Todo}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
