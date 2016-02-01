using Microsoft.AspNet.Mvc;

namespace _3DCytoFlow.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Instructions()
        {
            if (User.Identity.IsAuthenticated)
            {
                return View();
            }

            return RedirectToAction("LogIn", "Account");
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
