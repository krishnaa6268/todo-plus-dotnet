using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TodoPlus.Data;
using TodoPlus.Models;

namespace TodoPlus.Controllers
{
    public class TodoController : Controller
    {
        private readonly MongoDbContext _context;

        public TodoController(MongoDbContext context)
        {
            _context = context;
        }

        // GET: Todo
        public async Task<IActionResult> Index(string filter = "all", string? category = null, string? search = null, string sortBy = "dueDate")
        {
            ViewData["CurrentFilter"] = filter;
            ViewData["CurrentCategory"] = category;
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentSort"] = sortBy;

            try
            {
                var filterBuilder = Builders<TodoItem>.Filter;
                var today = DateTime.Today;

                // Overview Counts (computed across full dataset)
                var totalCount = await _context.TodoItems.CountDocumentsAsync(filterBuilder.Empty);
                var pendingCount = await _context.TodoItems.CountDocumentsAsync(filterBuilder.Eq(t => t.IsCompleted, false));
                var completedCount = await _context.TodoItems.CountDocumentsAsync(filterBuilder.Eq(t => t.IsCompleted, true));
                var overdueCount = await _context.TodoItems.CountDocumentsAsync(
                    filterBuilder.And(
                        filterBuilder.Eq(t => t.IsCompleted, false),
                        filterBuilder.Ne(t => t.DueDate, null),
                        filterBuilder.Lt(t => t.DueDate, today)
                    )
                );

                ViewData["TotalCount"] = (int)totalCount;
                ViewData["PendingCount"] = (int)pendingCount;
                ViewData["CompletedCount"] = (int)completedCount;
                ViewData["OverdueCount"] = (int)overdueCount;

                // Categories list for dropdown
                var categories = await _context.TodoItems.Distinct(t => t.Category, filterBuilder.Ne(t => t.Category, null)).ToListAsync();
                ViewData["Categories"] = categories.Where(c => !string.IsNullOrEmpty(c)).ToList()!;

                // Dynamic filter construction
                var filters = new List<FilterDefinition<TodoItem>>();

                // Search query filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchFilter = filterBuilder.Or(
                        filterBuilder.Regex(t => t.Title, new MongoDB.Bson.BsonRegularExpression(search, "i")),
                        filterBuilder.Regex(t => t.Description, new MongoDB.Bson.BsonRegularExpression(search, "i"))
                    );
                    filters.Add(searchFilter);
                }

                // Category filter
                if (!string.IsNullOrWhiteSpace(category))
                {
                    filters.Add(filterBuilder.Eq(t => t.Category, category));
                }

                // Status filter tab
                switch (filter.ToLower())
                {
                    case "active":
                        filters.Add(filterBuilder.Eq(t => t.IsCompleted, false));
                        break;
                    case "completed":
                        filters.Add(filterBuilder.Eq(t => t.IsCompleted, true));
                        break;
                    case "high":
                        filters.Add(filterBuilder.Eq(t => t.Priority, Priority.High));
                        break;
                    case "overdue":
                        filters.Add(filterBuilder.And(
                            filterBuilder.Eq(t => t.IsCompleted, false),
                            filterBuilder.Ne(t => t.DueDate, null),
                            filterBuilder.Lt(t => t.DueDate, today)
                        ));
                        break;
                    default:
                        // "all"
                        break;
                }

                var combinedFilter = filters.Count > 0 ? filterBuilder.And(filters) : filterBuilder.Empty;

                // Sort construction
                var sortBuilder = Builders<TodoItem>.Sort;
                SortDefinition<TodoItem> sort = sortBy switch
                {
                    "priority" => sortBuilder.Descending(t => t.Priority).Ascending(t => t.DueDate),
                    "created" => sortBuilder.Descending(t => t.CreatedAt),
                    "title" => sortBuilder.Ascending(t => t.Title),
                    _ => sortBuilder.Ascending(t => t.IsCompleted).Ascending(t => t.DueDate).Descending(t => t.Priority)
                };

                var items = await _context.TodoItems.Find(combinedFilter).Sort(sort).ToListAsync();
                return View(items);
            }
            catch (MongoAuthenticationException)
            {
                ViewData["ErrorMessage"] = "MongoDB Authentication Failed: The username or password in appsettings.json was rejected by MongoDB Atlas. Please check your Atlas credentials.";
                ViewData["TotalCount"] = 0;
                ViewData["PendingCount"] = 0;
                ViewData["CompletedCount"] = 0;
                ViewData["OverdueCount"] = 0;
                ViewData["Categories"] = new List<string>();
                return View(new List<TodoItem>());
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = $"MongoDB Connection Error: {ex.Message}";
                ViewData["TotalCount"] = 0;
                ViewData["PendingCount"] = 0;
                ViewData["CompletedCount"] = 0;
                ViewData["OverdueCount"] = 0;
                ViewData["Categories"] = new List<string>();
                return View(new List<TodoItem>());
            }
        }

        // GET: Todo/Details/56d...
        public async Task<IActionResult> Details(string? id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var item = await _context.TodoItems.Find(t => t.Id == id).FirstOrDefaultAsync();
            if (item == null) return NotFound();

            return View(item);
        }

        // GET: Todo/Create
        public IActionResult Create()
        {
            var model = new TodoItem
            {
                DueDate = DateTime.Today.AddDays(1),
                Priority = Priority.Medium
            };
            return View(model);
        }

        // POST: Todo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,DueDate,Priority,Category")] TodoItem item)
        {
            if (ModelState.IsValid)
            {
                item.CreatedAt = DateTime.Now;
                item.IsCompleted = false;
                await _context.TodoItems.InsertOneAsync(item);
                TempData["SuccessMessage"] = "Task created successfully in MongoDB!";
                return RedirectToAction(nameof(Index));
            }
            return View(item);
        }

        // GET: Todo/Edit/56d...
        public async Task<IActionResult> Edit(string? id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var item = await _context.TodoItems.Find(t => t.Id == id).FirstOrDefaultAsync();
            if (item == null) return NotFound();

            return View(item);
        }

        // POST: Todo/Edit/56d...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Title,Description,IsCompleted,DueDate,Priority,Category,CreatedAt,CompletedAt")] TodoItem item)
        {
            if (id != item.Id) return NotFound();

            if (ModelState.IsValid)
            {
                if (item.IsCompleted && !item.CompletedAt.HasValue)
                {
                    item.CompletedAt = DateTime.Now;
                }
                else if (!item.IsCompleted)
                {
                    item.CompletedAt = null;
                }

                var result = await _context.TodoItems.ReplaceOneAsync(t => t.Id == id, item);
                if (result.MatchedCount == 0)
                {
                    return NotFound();
                }

                TempData["SuccessMessage"] = "Task updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(item);
        }

        // POST: Todo/ToggleComplete/56d...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleComplete(string id, string? returnUrl)
        {
            var item = await _context.TodoItems.Find(t => t.Id == id).FirstOrDefaultAsync();
            if (item != null)
            {
                item.IsCompleted = !item.IsCompleted;
                item.CompletedAt = item.IsCompleted ? DateTime.Now : null;
                await _context.TodoItems.ReplaceOneAsync(t => t.Id == id, item);
                TempData["SuccessMessage"] = item.IsCompleted ? $"Completed '{item.Title}'!" : $"Reopened '{item.Title}'.";
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Todo/Delete/56d...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _context.TodoItems.DeleteOneAsync(t => t.Id == id);
            if (result.DeletedCount > 0)
            {
                TempData["SuccessMessage"] = "Task deleted successfully from MongoDB!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
