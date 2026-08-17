# TodoPlus - ASP.NET Core MVC Todo Application with MongoDB 📝

**TodoPlus** (TaskFlow) is a modern, responsive, and feature-rich Todo Application built with **ASP.NET Core 10.0 MVC** and **MongoDB** (supporting both MongoDB Atlas Cloud & local MongoDB instances) via the official **`MongoDB.Driver`**.

---

## 🌟 Key Features

- **🍃 MongoDB Database Integration**:
  - Direct connection to MongoDB Atlas Cloud / local MongoDB database named **`todo-csharp`**.
  - Document-based storage in the **`TodoItems`** collection using 24-character BSON ObjectIds.
  - Automatic collection initialization and initial sample dataset seeding on startup.

- **📊 Interactive Dashboard & KPI Metrics**:
  - Real-time statistics showing **Total**, **Pending**, **Completed**, and **Overdue** tasks.
  - Overall completion percentage progress bar.

- **⚡ Complete Task Lifecycle (CRUD)**:
  - **Create**: Add new tasks with title, description, due date, category, and priority level.
  - **Quick Toggle**: Rapid one-click completion status toggle from the task list.
  - **Edit & Update**: Modify task details, dates, categories, and completion status.
  - **Details View**: Comprehensive breakdown card for individual tasks.
  - **Delete**: Safely delete tasks from MongoDB with confirmation dialogs.

- **🔍 Search, Filtering & Sorting**:
  - Filter by status tabs: `All`, `Active`, `Completed`, `High Priority`, and `Overdue`.
  - Filter tasks by **Category** dropdown (e.g., Work, Personal, Learning).
  - Search tasks by title or description keywords using MongoDB Regex filters.
  - Sort by **Due Date**, **Priority**, **Date Created**, or **Title**.

- **🏷️ Priority & Category Management**:
  - Categorize tasks into custom domains (Work, Personal, Shopping, etc.).
  - Tag tasks with `Low`, `Medium`, or `High` priority levels serialized as strings in BSON documents.

- **🎨 Modern Aesthetic UI**:
  - Built using Bootstrap 5 + Bootstrap Icons.
  - Custom CSS styling with hover animations, priority badges, and glassmorphism elements.

---

## 🛠️ Technology Stack

| Component | Technology |
| :--- | :--- |
| **Framework** | ASP.NET Core 10.0 MVC |
| **Language** | C# 13 |
| **Database** | MongoDB (`todo-csharp` database / `TodoItems` collection) |
| **Driver / SDK** | `MongoDB.Driver` 3.11.0 |
| **Data Mapping** | BSON Serializers (`BsonId`, `BsonRepresentation`, `BsonDateTimeOptions`) |
| **Frontend** | Razor Views (`.cshtml`), HTML5, CSS3, JavaScript |
| **Styling** | Bootstrap 5, Bootstrap Icons |
| **Version Control** | Git & GitHub (`main` branch) |

---

## 📂 Project Structure

```text
TodoPlus/
├── Controllers/
│   ├── HomeController.cs        # Handles default route and error views
│   └── TodoController.cs        # Handles MongoDB CRUD operations, filters, sorts, and stats
├── Data/
│   └── MongoDbContext.cs        # Encapsulates IMongoDatabase, IMongoCollection, and seed logic
├── Models/
│   ├── ErrorViewModel.cs        # Error presentation model
│   ├── MongoDbSettings.cs       # Strongly-typed configuration class for MongoDB options
│   ├── Priority.cs              # Priority Enum (Low, Medium, High)
│   └── TodoItem.cs              # Todo model with BSON attributes (BsonId, BsonRepresentation)
├── Views/
│   ├── Shared/
│   │   └── _Layout.cshtml       # Main layout template with navbar and styling links
│   └── Todo/
│       ├── Index.cshtml         # Dashboard view with KPI cards, search, filters & task list
│       ├── Create.cshtml        # Task creation form
│       ├── Edit.cshtml          # Task update form
│       └── Details.cshtml       # Single task details view card
├── wwwroot/
│   └── css/
│       └── site.css             # Custom styling, badges, and card animations
├── .gitignore                   # Comprehensive .NET & OS git ignore rules
├── appsettings.json             # MongoDB connection string and database configuration
├── Program.cs                   # App entry point, DI services, MongoDB setup & routing
└── TodoPlus.csproj              # Project configuration and NuGet packages
```

---

## 🚀 Getting Started

### Prerequisites

Ensure you have the following installed on your machine:
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or .NET 8/9+ SDK)
- Access to a **MongoDB Atlas Cluster** URI or a local MongoDB instance (`mongodb://localhost:27017`).
- [Git](https://git-scm.com/)

### 📥 Repository Setup

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/krishnaa6268/todo-plus-dotnet.git
   cd todo-plus-dotnet/TodoPlus
   ```

2. **⚙️ MongoDB Configuration**:

   Configure your MongoDB Atlas or local connection string in `appsettings.json`:

   ```json
   {
     "MongoDbSettings": {
       "ConnectionString": "mongodb+srv://<username>:<password>@cluster0.cvxptsk.mongodb.net",
       "DatabaseName": "todo-csharp",
       "CollectionName": "TodoItems"
     }
   }
   ```

3. **Restore Dependencies**:
   ```bash
   dotnet restore
   ```

4. **Build the Project**:
   ```bash
   dotnet build
   ```

5. **Run the Application**:
   ```bash
   dotnet run
   ```

6. **Open in Browser**:
   Navigate to `http://localhost:5270` in your web browser.

---

## 🔌 API & Controller Endpoints

| HTTP Method | Route | Description |
| :--- | :--- | :--- |
| `GET` | `/` or `/Todo` | Task Dashboard with filtering, searching, and sorting |
| `GET` | `/Todo/Create` | Render Create Task form |
| `POST` | `/Todo/Create` | Submit and save new task to MongoDB |
| `GET` | `/Todo/Edit/{id}` | Render Edit Task form for BSON ObjectId `id` |
| `POST` | `/Todo/Edit/{id}` | Update existing task document in MongoDB |
| `GET` | `/Todo/Details/{id}` | View details for a single task |
| `POST` | `/Todo/ToggleComplete/{id}`| Toggle task completion status in MongoDB |
| `POST` | `/Todo/Delete/{id}` | Delete a task document from MongoDB |

---

## 🛡️ Database Seeding & Data Model Mapping

### BSON Data Annotations
In `Models/TodoItem.cs`, document fields are mapped to BSON types as follows:

```csharp
public class TodoItem
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsCompleted { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime? DueDate { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Priority Priority { get; set; } = Priority.Medium;

    public string? Category { get; set; } = "General";

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime? CompletedAt { get; set; }
}
```

### Automated Seeding Logic
In `Program.cs` and `MongoDbContext.cs`, the application checks if the `TodoItems` collection in database `todo-csharp` is empty upon startup. If empty, it populates initial sample task documents automatically.

---

## 📄 License

This project is open-source and available under the [MIT License](LICENSE).

