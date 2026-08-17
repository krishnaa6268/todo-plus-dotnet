using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TodoPlus.Models;

namespace TodoPlus.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly string _collectionName;
        private readonly ILogger<MongoDbContext>? _logger;

        public MongoDbContext(IOptions<MongoDbSettings> settings, ILogger<MongoDbContext>? logger = null)
        {
            _logger = logger;
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
            _collectionName = settings.Value.CollectionName;
        }

        public IMongoCollection<TodoItem> TodoItems => 
            _database.GetCollection<TodoItem>(_collectionName);

        public async Task SeedDataAsync()
        {
            try
            {
                var count = await TodoItems.CountDocumentsAsync(FilterDefinition<TodoItem>.Empty);
                if (count == 0)
                {
                    var initialItems = new List<TodoItem>
                    {
                        new TodoItem
                        {
                            Title = "Complete TodoPlus ASP.NET Core MVC App",
                            Description = "Build a fully working MVC application connected to a MongoDB database named todo-csharp.",
                            IsCompleted = false,
                            DueDate = DateTime.Today.AddDays(1),
                            Priority = Priority.High,
                            Category = "Work",
                            CreatedAt = DateTime.Now.AddDays(-1)
                        },
                        new TodoItem
                        {
                            Title = "Review MongoDB C# Driver Query Syntax",
                            Description = "Explore FilterDefinition, UpdateDefinition, and BsonDocument mapping in MongoDB C# Driver.",
                            IsCompleted = true,
                            DueDate = DateTime.Today.AddDays(-1),
                            Priority = Priority.Medium,
                            Category = "Learning",
                            CreatedAt = DateTime.Now.AddDays(-3),
                            CompletedAt = DateTime.Now.AddDays(-1)
                        },
                        new TodoItem
                        {
                            Title = "Setup MongoDB Connection & Collections",
                            Description = "Configure MongoDbSettings in appsettings.json and connect to todo-csharp database.",
                            IsCompleted = true,
                            DueDate = DateTime.Today,
                            Priority = Priority.High,
                            Category = "Work",
                            CreatedAt = DateTime.Now.AddDays(-2),
                            CompletedAt = DateTime.Now
                        },
                        new TodoItem
                        {
                            Title = "Buy Grocery Supplies",
                            Description = "Milk, Eggs, Coffee beans, fresh vegetables, and fruits.",
                            IsCompleted = false,
                            DueDate = DateTime.Today.AddDays(2),
                            Priority = Priority.Low,
                            Category = "Personal",
                            CreatedAt = DateTime.Now
                        }
                    };

                    await TodoItems.InsertManyAsync(initialItems);
                }
            }
            catch (MongoAuthenticationException ex)
            {
                _logger?.LogError(ex, "MongoDB Authentication failed. Please check the username and password in appsettings.json.");
                Console.WriteLine("⚠️ [MongoDB Auth Error]: Invalid credentials in appsettings.json. Check username and password.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MongoDB connection or seeding failed.");
                Console.WriteLine($"⚠️ [MongoDB Error]: {ex.Message}");
            }
        }
    }
}
