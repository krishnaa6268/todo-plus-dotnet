# TodoPlus - ASP.NET Core MVC Todo Application with JWT Auth & Admin Dashboard

**TodoPlus** is a modern, responsive, and feature-rich Todo Application built with **ASP.NET Core 10.0 MVC**, **JWT Authentication**, and **MongoDB** (supporting both MongoDB Atlas Cloud & local MongoDB instances) via the official **`MongoDB.Driver`**.

---

## Key Features

## 1. JWT Authentication & Security
- **Secure Sign Up & Log In**: User authentication using **JSON Web Tokens (JWT)** and **BCrypt** password hashing.
- **Dual Token Handling**: Works out-of-the-box for both **Web Browsers** (via HttpOnly `JwtToken` cookie) and **REST API Clients / Mobile Apps** (via `Authorization: Bearer <token>` header).
- **Default Seed Accounts**:
  - **Admin Account**: `admin**@todoplus.com` / `********` (Role: `Admin`)
  - **Demo User Account**: `user**@todoplus.com` / `********` (Role: `User`)

---

### 2. User-Scoped Todo Lists (Strict Privacy & Data Scoping)
- **Isolated User Workspaces**: Every user sees, creates, and manages **only their own tasks**.
- **Access Control & Authorization**: Strict authorization checks enforce that regular users cannot view, edit, or delete another user's task by ID (returns `403 Forbidden`).
- **Productivity Tracker**: Real-time progress bar, completion stats, priority badges (`Low`, `Medium`, `High`), categories, and due date tracking.
- **Full CRUD Operations**: Create, Edit, Toggle Completion, View Details, and Delete tasks.
- **Search & Filtering**: Search by title/description, filter by status (`All`, `Active`, `Completed`, `High Priority`, `Overdue`), filter by category, and sort dynamically.

---

### 3. Admin Control Center (User Records & System Audit)
- **Registered User Directory (`/Admin/Users`)**: Admins can view complete records for all registered users:
  - User ID, Username, Email Address, Role (`Admin` / `User`), and Registration Date.
  - Live task breakdown: Total Tasks, Completed Tasks, and Pending Tasks per user.
- **Admin Management Actions**:
  - **Live Search**: Search users by username or email.
  - **Switch Role**: Toggle user permissions between `User` and `Admin` with 1 click.
  - **Delete Account**: Remove user account and automatically clean up associated tasks.
- **System Task Audit (`/Admin/AllTodos`)**: Admins can audit all tasks across all users in the system or filter tasks by specific user.

---

### 4. Modern Glassmorphism UI
- **Responsive Layout**: Designed with modern CSS variables, vibrant gradients, glassmorphism cards, and Bootstrap Icons.
- **Dynamic Header & Badges**: Navbar automatically displays current user username, avatar circle, role badge (`Admin` or `User`), and Admin navigation links.
- **Toast Alerts**: Non-intrusive notification popups for login, logout, task actions, and permission errors.

---

## Technology Stack

| Component | Technology |
| :--- | :--- |
| **Framework** | ASP.NET Core 10.0 MVC |
| **Language** | C# 13 |
| **Authentication** | JWT (JSON Web Token) via `Microsoft.AspNetCore.Authentication.JwtBearer` |
| **Password Hashing** | `BCrypt.Net-Next` |
| **Database** | MongoDB (`todo-csharp` database / `TodoItems` & `Users` collections) |
| **Database Driver** | `MongoDB.Driver` 3.11.0 |
| **Environment Configuration** | `DotNetEnv` 3.2.0 (`.env` file) |
| **Data Mapping** | BSON Serializers (`BsonId`, `BsonRepresentation`, `BsonDateTimeOptions`) |
| **Frontend** | Razor Views (`.cshtml`), HTML5, CSS3, JavaScript |
| **Styling** | Bootstrap 5, Bootstrap Icons, Custom Glassmorphism CSS |

---

## Environment Variables (`.env`)

Environment variables are loaded automatically on application startup from the `.env` file in the root directory.

### `.env` File Example:

```env
# --- MongoDB Connection Settings ---
MONGODB_URI=mongodb+srv://<username>:<password>@cluster.mongodb.net
MONGODB_DATABASE=todo-csharp
MONGODB_COLLECTION=TodoItems

# --- JWT Authentication Settings ---
JWT_SECRET_KEY=SuperSecretTodoPlusJwtSigningKey2026WithAtLeast256BitsOfEntropy!!
JWT_ISSUER=TodoPlusApp
JWT_AUDIENCE=TodoPlusUsers
JWT_EXPIRATION_MINUTES=1440
```

---

## REST API Endpoints

In addition to MVC Razor Views, **TodoPlus** provides RESTful API endpoints for authentication:

| Method | Endpoint | Description | Request Body / Header |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/login` | Authenticates user & returns JWT JSON | `{ "emailOrUsername": "...", "password": "..." }` |
| `POST` | `/api/auth/register` | Registers user & returns JWT JSON | `{ "username": "...", "email": "...", "password": "...", "confirmPassword": "..." }` |

---

## Project Structure

```text
TodoPlus/
├── Controllers/
│   ├── AccountController.cs    # Login, Signup, Logout, and REST API auth endpoints
│   ├── AdminController.cs      # Admin panel for user records directory & task audit
│   ├── HomeController.cs       # Default route and error page handler
│   └── TodoController.cs       # User-scoped task CRUD, filtering, search & sorting
├── Data/
│   └── MongoDbContext.cs       # MongoDB collections, unique indexes & seed data
├── Models/
│   ├── AuthViewModels.cs       # Login, Register, JWT Response, and Admin User view models
│   ├── ErrorViewModel.cs       # Error model
│   ├── MongoDbSettings.cs      # MongoDB configuration options
│   ├── Priority.cs             # Priority enum (Low, Medium, High)
│   ├── TodoItem.cs             # Task model with UserId & OwnerUsername mapping
│   └── User.cs                 # User model with Roles ("Admin", "User")
├── Services/
│   └── JwtService.cs           # IJwtService for generating & validating JWT tokens
├── Views/
│   ├── Account/
│   │   ├── AccessDenied.cshtml # Unauthorized Admin access view
│   │   ├── Login.cshtml        # Login view with quick demo fill buttons
│   │   └── Register.cshtml     # Registration view
│   ├── Admin/
│   │   ├── AllTodos.cshtml     # System-wide task audit view for Admins
│   │   └── Users.cshtml        # User records management table & metrics
│   ├── Shared/
│   │   └── _Layout.cshtml      # Site layout with navbar, user avatar, and toasts
│   └── Todo/
│       ├── Create.cshtml       # Task creation form
│       ├── Details.cshtml      # Single task view card
│       ├── Edit.cshtml         # Task update form
│       └── Index.cshtml        # User dashboard with progress bar & task cards
├── wwwroot/
│   └── css/
│       └── site.css            # Modern glassmorphism CSS theme & custom styling
├── .env                        # Active environment variables (Git ignored)
├── .env.example                # Template for environment configuration
├── Program.cs                  # Entry point, JWT Bearer middleware & DI services
└── TodoPlus.csproj             # .NET 10 project file & NuGet dependencies
```

---

## How to Run Locally

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/your-username/todo-plus-dotnet.git
   cd todo-plus-dotnet
   ```

2. **Configure Environment Variables**:
   Copy `.env.example` to `.env` and set your MongoDB URI and JWT Secret Key:
   ```bash
   cp .env.example .env
   ```

3. **Run the Application**:
   ```bash
   dotnet run
   ```

4. **Access in Browser**:
   Navigate to `https://localhost:7198` (or the printed port).