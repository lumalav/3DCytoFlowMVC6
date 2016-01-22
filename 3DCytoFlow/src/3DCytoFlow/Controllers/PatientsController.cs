using System.Linq;
using Microsoft.AspNet.Mvc;
using _3DCytoFlow.Models;

namespace _3DCytoFlow.Controllers
{
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientsController(ApplicationDbContext context)
        {
            _context = context;    
        }

        // GET: Patients
        public ActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = GetUser();

                return View(_context.Patients.Where(i => i.User.Id.Equals(user.Id)).ToList());
            }

            return RedirectToAction("LogIn", "Account");
        }

        // GET: Patients/Details/5
        public IActionResult Details(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            Patient patient = _context.Patients.Single(m => m.Id == id);
            if (patient == null)
            {
                return HttpNotFound();
            }

            return View(patient);
        }

        // GET: Patients/Create
        public IActionResult Create()
        {
            if (User.Identity.IsAuthenticated)
            {
                return View();
            }

            return RedirectToAction("LogIn", "Account");
        }

        // POST: Patients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Id,FirstName,Middle,LastName,DOB,Email,Phone,Address,City,Zip")] Patient patient)
        {
            if (ModelState.IsValid)
            {
                var user = GetUser();
                user.Patients.Add(patient);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(patient);
        }

        // GET: Patients/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            var patient = _context.Patients.Single(m => m.Id == id);
            if (patient == null)
            {
                return HttpNotFound();
            }
            return View(patient);
        }

        // POST: Patients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit([Bind("Id,FirstName,Middle,LastName,DOB,Email,Phone,Address,City,Zip")] Patient patient)
        {
            if (ModelState.IsValid)
            {
                var user = GetUser();
                patient.User = user;
                _context.Update(patient);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(patient);
        }

        // GET: Patients/Delete/5
        [ActionName("Delete")]
        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            var patient = _context.Patients.Single(m => m.Id == id);
            if (patient == null)
            {
                return HttpNotFound();
            }

            return View(patient);
        }

        // POST: Patients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            Patient patient = _context.Patients.Single(m => m.Id == id);
            _context.Patients.Remove(patient);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        #region Helpers
        private ApplicationUser GetUser()
        {
            return _context.Users.First(i => i.UserName.Equals(User.Identity.Name));
        }
        #endregion
    }
}
