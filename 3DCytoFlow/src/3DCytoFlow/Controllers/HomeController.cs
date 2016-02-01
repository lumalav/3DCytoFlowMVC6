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
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
