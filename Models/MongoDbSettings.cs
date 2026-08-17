namespace TodoPlus.Models
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "todo-csharp";
        public string CollectionName { get; set; } = "TodoItems";
    }
}
