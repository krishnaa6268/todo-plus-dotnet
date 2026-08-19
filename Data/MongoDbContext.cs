using BCrypt.Net;
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

        public IMongoCollection<User> Users => 
            _database.GetCollection<User>("Users");

        public async Task SeedDataAsync()
        {
            try
            {
                // Create unique indexes for Users collection
                var emailIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
                var emailIndexModel = new CreateIndexModel<User>(emailIndexKeys, new CreateIndexOptions { Unique = true });

                var usernameIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Username);
                var usernameIndexModel = new CreateIndexModel<User>(usernameIndexKeys, new CreateIndexOptions { Unique = true });

                await Users.Indexes.CreateManyAsync(new[] { emailIndexModel, usernameIndexModel });

                // Seed Default Admin User
                var adminUser = await Users.Find(u => u.Email == "admin@todoplus.com" || u.Username == "admin").FirstOrDefaultAsync();
                if (adminUser == null)
                {
                    adminUser = new User
                    {
                        Username = "admin",
                        Email = "admin@todoplus.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                        Role = Roles.Admin,
                        CreatedAt = DateTime.Now
                    };
                    await Users.InsertOneAsync(adminUser);
                    _logger?.LogInformation("Seeded default Admin account: admin@todoplus.com");
                }

                // Seed Default Standard Demo User
                var demoUser = await Users.Find(u => u.Email == "user@todoplus.com" || u.Username == "demo_user").FirstOrDefaultAsync();
                if (demoUser == null)
                {
                    demoUser = new User
                    {
                        Username = "demo_user",
                        Email = "user@todoplus.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                        Role = Roles.User,
                        CreatedAt = DateTime.Now
                    };
                    await Users.InsertOneAsync(demoUser);
                    _logger?.LogInformation("Seeded default Demo User account: user@todoplus.com");
                }

                // Seed or Update TodoItems
                var count = await TodoItems.CountDocumentsAsync(FilterDefinition<TodoItem>.Empty);
                if (count == 0)
                {
                    var initialItems = new List<TodoItem>
                    {
                        new TodoItem
                        {
                            Title = "Welcome to TodoPlus JWT App",
                            Description = "Build a fully working ASP.NET Core MVC application with JWT Authentication & User management.",
                            IsCompleted = false,
                            DueDate = DateTime.Today.AddDays(1),
                            Priority = Priority.High,
                            Category = "Work",
                            UserId = demoUser.Id,
                            OwnerUsername = demoUser.Username,
                            CreatedAt = DateTime.Now.AddDays(-1)
                        },
                        new TodoItem
                        {
                            Title = "Review MongoDB & Auth Queries",
                            Description = "Explore user-scoped todo listing, role-based filtering, and JWT token issuance.",
                            IsCompleted = true,
                            DueDate = DateTime.Today.AddDays(-1),
                            Priority = Priority.Medium,
                            Category = "Learning",
                            UserId = demoUser.Id,
                            OwnerUsername = demoUser.Username,
                            CreatedAt = DateTime.Now.AddDays(-3),
                            CompletedAt = DateTime.Now.AddDays(-1)
                        },
                        new TodoItem
                        {
                            Title = "Setup Admin Management Dashboard",
                            Description = "Configure user role toggling, viewing total task counts, and system metrics.",
                            IsCompleted = false,
                            DueDate = DateTime.Today,
                            Priority = Priority.High,
                            Category = "Work",
                            UserId = adminUser.Id,
                            OwnerUsername = adminUser.Username,
                            CreatedAt = DateTime.Now.AddDays(-2)
                        },
                        new TodoItem
                        {
                            Title = "Buy Grocery Supplies",
                            Description = "Milk, Eggs, Coffee beans, fresh vegetables, and fruits.",
                            IsCompleted = false,
                            DueDate = DateTime.Today.AddDays(2),
                            Priority = Priority.Low,
                            Category = "Personal",
                            UserId = demoUser.Id,
                            OwnerUsername = demoUser.Username,
                            CreatedAt = DateTime.Now
                        }
                    };

                    await TodoItems.InsertManyAsync(initialItems);
                }
                else
                {
                    // Backfill any unassigned TodoItems with demoUser's ID
                    var unassignedFilter = Builders<TodoItem>.Filter.Or(
                        Builders<TodoItem>.Filter.Eq(t => t.UserId, null),
                        Builders<TodoItem>.Filter.Eq(t => t.UserId, "")
                    );
                    var update = Builders<TodoItem>.Update
                        .Set(t => t.UserId, demoUser.Id)
                        .Set(t => t.OwnerUsername, demoUser.Username);

                    await TodoItems.UpdateManyAsync(unassignedFilter, update);
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
