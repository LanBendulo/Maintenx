using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IT15_Project.Controllers
{
    /// <summary>
    /// Dashboard for authenticated non-admin users.
    /// Any unauthenticated request is redirected to login.
    /// </summary>
    [Authorize]
    public class UserDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult MyRequests()
        {
            return View();
        }

        public IActionResult SubmitRequest()
        {
            return View();
        }

        public IActionResult Notifications()
        {
            return View();
        }
    }
}
