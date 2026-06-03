using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementApp.Controllers
{
    public class PagesController : Controller
    {
        // GET: /Pages/Resources
        public IActionResult Resources()
        {
            return View();
        }

        // GET: /Pages/Legal
        public IActionResult Legal()
        {
            return View();
        }
    }
}
