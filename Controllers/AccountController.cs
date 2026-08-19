using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TodoPlus.Data;
using TodoPlus.Models;
using TodoPlus.Services;

namespace TodoPlus.Controllers
{
    public class AccountController : Controller
    {
        private readonly MongoDbContext _context;
        private readonly IJwtService _jwtService;

        public AccountController(MongoDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Todo");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var input = model.EmailOrUsername.Trim().ToLower();
            var user = await _context.Users.Find(u => 
                u.Email.ToLower() == input || u.Username.ToLower() == input
            ).FirstOrDefaultAsync();

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid email/username or password.");
                return View(model);
            }

            var token = _jwtService.GenerateToken(user);

            // Store JWT in HTTP cookie for MVC views navigation
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(24)
            };

            Response.Cookies.Append("JwtToken", token, cookieOptions);
            TempData["ToastMessage"] = $"Welcome back, {user.Username}!";
            TempData["ToastType"] = "success";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Todo");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Todo");
            }

            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var emailClean = model.Email.Trim().ToLower();
            var usernameClean = model.Username.Trim().ToLower();

            // Check existing user
            var existingUser = await _context.Users.Find(u => 
                u.Email.ToLower() == emailClean || u.Username.ToLower() == usernameClean
            ).FirstOrDefaultAsync();

            if (existingUser != null)
            {
                if (existingUser.Email.ToLower() == emailClean)
                {
                    ModelState.AddModelError("Email", "An account with this email already exists.");
                }
                if (existingUser.Username.ToLower() == usernameClean)
                {
                    ModelState.AddModelError("Username", "This username is already taken.");
                }
                return View(model);
            }

            var user = new User
            {
                Username = model.Username.Trim(),
                Email = model.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = model.IsAdmin ? Roles.Admin : Roles.User,
                CreatedAt = DateTime.Now
            };

            await _context.Users.InsertOneAsync(user);

            var token = _jwtService.GenerateToken(user);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            };

            Response.Cookies.Append("JwtToken", token, cookieOptions);
            TempData["ToastMessage"] = $"Account created successfully! Welcome, {user.Username}.";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index", "Todo");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("JwtToken");
            TempData["ToastMessage"] = "You have been logged out successfully.";
            TempData["ToastType"] = "info";
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // --- REST API ENDPOINTS FOR API CLIENTS ---

        [HttpPost("api/auth/login")]
        public async Task<IActionResult> ApiLogin([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var input = model.EmailOrUsername.Trim().ToLower();
            var user = await _context.Users.Find(u => 
                u.Email.ToLower() == input || u.Username.ToLower() == input
            ).FirstOrDefaultAsync();

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email/username or password." });
            }

            var token = _jwtService.GenerateToken(user);
            return Ok(new JwtAuthResponseDto
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                Expiration = DateTime.UtcNow.AddHours(24)
            });
        }

        [HttpPost("api/auth/register")]
        public async Task<IActionResult> ApiRegister([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var emailClean = model.Email.Trim().ToLower();
            var usernameClean = model.Username.Trim().ToLower();

            var existingUser = await _context.Users.Find(u => 
                u.Email.ToLower() == emailClean || u.Username.ToLower() == usernameClean
            ).FirstOrDefaultAsync();

            if (existingUser != null)
            {
                return Conflict(new { message = "Email or Username is already registered." });
            }

            var user = new User
            {
                Username = model.Username.Trim(),
                Email = model.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = model.IsAdmin ? Roles.Admin : Roles.User,
                CreatedAt = DateTime.Now
            };

            await _context.Users.InsertOneAsync(user);
            var token = _jwtService.GenerateToken(user);

            return Ok(new JwtAuthResponseDto
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                Expiration = DateTime.UtcNow.AddHours(24)
            });
        }
    }
}
