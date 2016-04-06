using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using _3DCytoFlow.Models;
using _3DCytoFlow.ViewModels.VirtualMachine;

namespace _3DCytoFlow.Controllers
{
    [Authorize(Roles ="Admin")]
    public class VirtualMachinesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VirtualMachinesController(ApplicationDbContext context)
        {
            _context = context;    
        }

        // GET: VirtualMachines
        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return View(_context.VirtualMachines.ToList().Select(x => new DetailsViewModel { id = x.Id, name = x.MachineName }));
            }
            return RedirectToAction("LogIn", "Account");
        }

        // GET: VirtualMachines/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            if (User.Identity.IsAuthenticated)
            {
                return View();
            }

            return RedirectToAction("LogIn", "Account");
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Hash the password
                var psw = PasswordHash.CreateHash(model.Password);

                var machine = new VirtualMachine
                {
                    MachineName = model.MachineName,
                    HashedPassword = psw
                };

                _context.VirtualMachines.Add(machine);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "VirtualMachines", null);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetToken([FromQuery]string username, string psw)
        {
            var vm = _context.VirtualMachines.First(x => x.MachineName == username);

            if (vm != null)
            {
                if (PasswordHash.ValidatePassword(psw, vm.HashedPassword))
                {
                    return Json(vm.Id);
                }
                return HttpBadRequest();
            }

            return HttpNotFound();
        }

        // GET: VirtualMachines/Delete/5
        [ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string id)
        {
            if (User.Identity.IsAuthenticated)
            {
                if (id == null)
                {
                    return HttpNotFound();
                }

                VirtualMachine virtualMachine = _context.VirtualMachines.Single(m => m.Id == id);
                if (virtualMachine == null)
                {
                    return HttpNotFound();
                }

                return View(new DetailsViewModel { id = virtualMachine.Id, name = virtualMachine.MachineName });
            }
            return RedirectToAction("LogIn", "Account");
        }

        // POST: VirtualMachines/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id)
        {
            VirtualMachine virtualMachine = _context.VirtualMachines.Single(m => m.Id == id);
            _context.VirtualMachines.Remove(virtualMachine);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
