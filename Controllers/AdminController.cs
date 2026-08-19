using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TodoPlus.Data;
using TodoPlus.Models;

namespace TodoPlus.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class AdminController : Controller
    {
        private readonly MongoDbContext _context;

        public AdminController(MongoDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Users(string? search = null)
        {
            ViewData["CurrentSearch"] = search;

            var filterBuilder = Builders<User>.Filter;
            var filter = filterBuilder.Empty;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                filter = filterBuilder.Or(
                    filterBuilder.Regex(u => u.Username, new MongoDB.Bson.BsonRegularExpression(s, "i")),
                    filterBuilder.Regex(u => u.Email, new MongoDB.Bson.BsonRegularExpression(s, "i"))
                );
            }

            var users = await _context.Users.Find(filter).SortByDescending(u => u.CreatedAt).ToListAsync();

            // Fetch all todos stats per user
            var allTodos = await _context.TodoItems.Find(Builders<TodoItem>.Filter.Empty).ToListAsync();

            var userRecords = users.Select(u => new UserRecordViewModel
            {
                Id = u.Id ?? string.Empty,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                TotalTasks = allTodos.Count(t => t.UserId == u.Id),
                CompletedTasks = allTodos.Count(t => t.UserId == u.Id && t.IsCompleted),
                PendingTasks = allTodos.Count(t => t.UserId == u.Id && !t.IsCompleted)
            }).ToList();

            ViewData["TotalUsersCount"] = userRecords.Count;
            ViewData["AdminCount"] = userRecords.Count(u => u.Role == Roles.Admin);
            ViewData["StandardUserCount"] = userRecords.Count(u => u.Role == Roles.User);
            ViewData["TotalSystemTasks"] = allTodos.Count;

            return View(userRecords);
        }

        // GET: /Admin/AllTodos
        public async Task<IActionResult> AllTodos(string? userId = null, string filter = "all", string? search = null)
        {
            ViewData["SelectedUserId"] = userId;
            ViewData["CurrentFilter"] = filter;
            ViewData["CurrentSearch"] = search;

            var users = await _context.Users.Find(Builders<User>.Filter.Empty).ToListAsync();
            ViewData["UsersList"] = users;

            var filterBuilder = Builders<TodoItem>.Filter;
            var filters = new List<FilterDefinition<TodoItem>>();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                filters.Add(filterBuilder.Eq(t => t.UserId, userId));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                filters.Add(filterBuilder.Or(
                    filterBuilder.Regex(t => t.Title, new MongoDB.Bson.BsonRegularExpression(search, "i")),
                    filterBuilder.Regex(t => t.Description, new MongoDB.Bson.BsonRegularExpression(search, "i")),
                    filterBuilder.Regex(t => t.OwnerUsername, new MongoDB.Bson.BsonRegularExpression(search, "i"))
                ));
            }

            switch (filter.ToLower())
            {
                case "active":
                    filters.Add(filterBuilder.Eq(t => t.IsCompleted, false));
                    break;
                case "completed":
                    filters.Add(filterBuilder.Eq(t => t.IsCompleted, true));
                    break;
            }

            var combinedFilter = filters.Count > 0 ? filterBuilder.And(filters) : filterBuilder.Empty;
            var todos = await _context.TodoItems.Find(combinedFilter).SortByDescending(t => t.CreatedAt).ToListAsync();

            return View(todos);
        }

        // POST: /Admin/ToggleRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRole(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest();
            }

            var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
            {
                TempData["ToastMessage"] = "User not found.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Users));
            }

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (user.Id == currentUserId && user.Role == Roles.Admin)
            {
                TempData["ToastMessage"] = "You cannot change your own admin role!";
                TempData["ToastType"] = "warning";
                return RedirectToAction(nameof(Users));
            }

            var newRole = user.Role == Roles.Admin ? Roles.User : Roles.Admin;

            // If demoting admin, ensure at least one other admin remains
            if (user.Role == Roles.Admin && newRole == Roles.User)
            {
                var adminCount = await _context.Users.CountDocumentsAsync(u => u.Role == Roles.Admin);
                if (adminCount <= 1)
                {
                    TempData["ToastMessage"] = "Cannot demote the only remaining Admin!";
                    TempData["ToastType"] = "danger";
                    return RedirectToAction(nameof(Users));
                }
            }

            var update = Builders<User>.Update.Set(u => u.Role, newRole);
            await _context.Users.UpdateOneAsync(u => u.Id == userId, update);

            TempData["ToastMessage"] = $"Role for {user.Username} updated to {newRole}.";
            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(Users));
        }

        // POST: /Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest();
            }

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == currentUserId)
            {
                TempData["ToastMessage"] = "You cannot delete your own account from the Admin panel!";
                TempData["ToastType"] = "warning";
                return RedirectToAction(nameof(Users));
            }

            var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
            {
                TempData["ToastMessage"] = "User not found.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Users));
            }

            // Delete associated todo items
            await _context.TodoItems.DeleteManyAsync(t => t.UserId == userId);

            // Delete user
            await _context.Users.DeleteOneAsync(u => u.Id == userId);

            TempData["ToastMessage"] = $"User {user.Username} and their associated tasks were deleted.";
            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(Users));
        }
    }
}
