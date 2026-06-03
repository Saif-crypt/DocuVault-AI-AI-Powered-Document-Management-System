using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using DocumentManagementApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using DocumentManagementApp.Models;

namespace DocumentManagementApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly IPasswordHasher<IdentityUser> _passwordHasher;

        public AccountController(
            ApplicationDbContext context, 
            ILogger<AccountController> logger,
            IPasswordHasher<IdentityUser> passwordHasher)
        {
            _context = context;
            _logger = logger;
            _passwordHasher = passwordHasher;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Document");
            }
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Email and password are required");
                return View();
            }

            try
            {
                // Find user by email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid email or password");
                    return View();
                }

                // Verify password
                var result = _passwordHasher.VerifyHashedPassword(
                    user, 
                    user.PasswordHash ?? string.Empty, 
                    password);

                if (result == PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError("", "Invalid email or password");
                    return View();
                }

                // Sign in using Identity's SignInManager would be better, but since we're using cookie auth directly:
                var signInManager = HttpContext.RequestServices.GetRequiredService<SignInManager<IdentityUser>>();
                await signInManager.SignInAsync(user, isPersistent: true);

                return RedirectToAction("Dashboard", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                ModelState.AddModelError("", "An error occurred during login");
                return View();
            }
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Document");
            }
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword, string fullName)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Email and password are required");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match");
                return View();
            }

            if (password.Length < 6)
            {
                ModelState.AddModelError("", "Password must be at least 6 characters");
                return View();
            }

            try
            {
                // Check if user already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Email already registered");
                    return View();
                }

                // Create new user using UserManager
                var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
                
                var user = new IdentityUser
                {
                    Email = email,
                    UserName = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Registration successful! Please login.";
                    return RedirectToAction("Login");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration error");
                ModelState.AddModelError("", "An error occurred during registration");
                return View();
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? "user@example.com";
            var userName = User.Identity?.Name ?? "User";

            // Get all documents for the logged-in user
             var documents = await _context.Documents
            .Where(d => d.UserId == userId.ToString())
            .ToListAsync();

            // Try to get account creation date from Users table if available
            DateTime? accountCreatedDate = null;
        try
           {
            var user = await _context.Users.FindAsync(userId.ToString());
        // Note: This depends on your User model having a creation date field
        // If not available, this will just remain null
           }
        catch
    {
        // If Users table doesn't have creation date, that's OK
    }

    // Calculate statistics
    var viewModel = new ProfileViewModel
    {
        UserName = userName,
        Email = userEmail,
        AccountCreatedDate = accountCreatedDate,
        TotalDocuments = documents.Count,
        OcrProcessed = documents.Count(d => d.OcrStatus == "Completed"),
        NlpProcessed = documents.Count(d => d.NlpStatus == "Completed"),
        SummariesGenerated = documents.Count(d => !string.IsNullOrEmpty(d.ProcessedText))
    };

    return View(viewModel);
}

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var signInManager = HttpContext.RequestServices.GetRequiredService<SignInManager<IdentityUser>>();
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
                // Helper method
        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }
    }
}
