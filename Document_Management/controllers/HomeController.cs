using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using DocumentManagementApp.Data;
using DocumentManagementApp.Models;
using System.Diagnostics;


namespace DocumentManagementApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: / (Landing page - accessible to everyone)
        [AllowAnonymous]
        public IActionResult Index()
        {
            // If user is already logged in, redirect to dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard");
            }

            // Show landing page
            return View();
        }

        // GET: /Home/Dashboard (After login - Dashboard page)
[Authorize]
public async Task<IActionResult> Dashboard()
{
    var userId = GetCurrentUserId();
    ViewBag.UserName = User.Identity?.Name ?? "User";

    var documents = await _context.Documents
        .Where(d => d.UserId == userId)
        .OrderByDescending(d => d.UploadedAt)
        .ToListAsync();

    var viewModel = new DashboardViewModel
    {
        TotalDocuments = documents.Count,
        OcrCompleted = documents.Count(d => d.OcrStatus == "Completed"),
        NlpProcessed = documents.Count(d => d.NlpStatus == "Completed"),
        SummariesGenerated = documents.Count(d => !string.IsNullOrEmpty(d.ProcessedText)),
        RecentDocuments = documents.Take(3).ToList()
    };

    return View(viewModel);
}
        // Helper method
private string GetCurrentUserId()
{
    return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
}

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
}
