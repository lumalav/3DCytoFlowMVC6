using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Data.Entity;
using _3DCytoFlow.Models;
using Microsoft.AspNet.Authorization;
using System.Threading.Tasks;
using _3DCytoFlow.ViewModels.VirtualMachine;

namespace _3DCytoFlow.Controllers
{
    [Authorize(Roles ="Admin")]
    public class VirtualMachinesController : Controller
    {
        private ApplicationDbContext _context;

        public VirtualMachinesController(ApplicationDbContext context)
        {
            _context = context;    
        }

        // GET: VirtualMachines
        public IActionResult Index()
        {
            return View(_context.VirtualMachines.ToList().Select(x => new DetailsViewModel { id = x.Id, name = x.MachineName }));
        }

        // GET: VirtualMachines/Create
        public IActionResult Create()
        {
            return View();
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
                var psw = PasswordHash.PasswordHash.CreateHash(model.Password);

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

            if ( vm != null )
            {
                if( PasswordHash.PasswordHash.ValidatePassword(psw, vm.HashedPassword ) )
                {
                    return Json(vm.Id);
                }
                return HttpBadRequest();
            }

            return HttpNotFound();  
        }

        // GET: VirtualMachines/Delete/5
        [ActionName("Delete")]
        public IActionResult Delete(string id)
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

            return View(new DetailsViewModel { id = virtualMachine.Id, name = virtualMachine.MachineName } );
        }

        // POST: VirtualMachines/Delete/5
        [HttpPost, ActionName("Delete")]
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
