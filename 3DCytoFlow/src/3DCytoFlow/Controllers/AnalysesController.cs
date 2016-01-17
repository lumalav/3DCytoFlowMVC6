using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Data.Entity;
using _3DCytoFlow;
using _3DCytoFlow.Models;

namespace _3DCytoFlow.Controllers
{
    public class AnalysesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnalysesController(ApplicationDbContext context)
        {
            _context = context;    
        }

        // GET: Analyses
        public IActionResult Index()
        {
            return View(_context.Analyses.ToList());
        }

        // GET: Analyses/Details/5
        public IActionResult Details(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            Analysis analysis = _context.Analyses.Single(m => m.Id == id);
            if (analysis == null)
            {
                return HttpNotFound();
            }

            return View(analysis);
        }

        // GET: Analyses/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Analyses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Analysis analysis)
        {
            if (ModelState.IsValid)
            {
                _context.Analyses.Add(analysis);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(analysis);
        }

        // GET: Analyses/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            Analysis analysis = _context.Analyses.Single(m => m.Id == id);
            if (analysis == null)
            {
                return HttpNotFound();
            }
            return View(analysis);
        }

        // POST: Analyses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Analysis analysis)
        {
            if (ModelState.IsValid)
            {
                _context.Update(analysis);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(analysis);
        }

        // GET: Analyses/Delete/5
        [ActionName("Delete")]
        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            Analysis analysis = _context.Analyses.Single(m => m.Id == id);
            if (analysis == null)
            {
                return HttpNotFound();
            }

            return View(analysis);
        }

        // POST: Analyses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            Analysis analysis = _context.Analyses.Single(m => m.Id == id);
            _context.Analyses.Remove(analysis);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
