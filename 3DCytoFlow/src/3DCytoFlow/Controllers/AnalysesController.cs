using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using _3DCytoFlow.Models;
using _3DCytoFlow.ViewModels.Analysis;

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
            if (User.Identity.IsAuthenticated)
            {
                var user = GetUser();
                var analysisViews = new List<AnalysisViewModel>();

                foreach( var analysis in _context.Analyses.Include( x=>x.Patient ).Include(x=>x.User).Where(x=>x.User.Id == user.Id) )
                {
                    var patient = analysis.Patient;

                    analysisViews.Add(new AnalysisViewModel
                    {
                        Id = analysis.Id,
                        UserId = analysis.User.Id,
                        UserFirstName = analysis.User.FirstName,
                        UserLastName = analysis.User.LastName,
                        PatientId = patient.Id,
                        PatientFirstName = patient.FirstName,
                        PatientLastName = patient.LastName,
                        Date = analysis.ResultDate,
                        ResultFilePath = analysis.ResultFilePath
                    });
                }

                return View(analysisViews);
            }

            return RedirectToAction("LogIn", "Account");
        }

        // GET: Analyses/Delete/5
        [ActionName("Delete")]
        public IActionResult Delete(int? id)
        {
            if (User.Identity.IsAuthenticated)
            {
                if (id == null)
                {
                    return HttpNotFound();
                }

                var analysis = _context.Analyses.Single(m => m.Id == id);

                if (analysis == null)
                {
                    return HttpNotFound();
                }

                var userId = User.GetUserId();

                //TODO: FIX
                if (!analysis.User.Id.Equals(userId))
                {
                    ViewBag.errorMessage = "You don't have permission to do that";
                    return View("Error");
                }

                return View(analysis);
            }

            return RedirectToAction("LogIn", "Account");
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

        #region Helpers
        /// <summary>
        /// returns the current user
        /// </summary>
        /// <returns></returns>
        private ApplicationUser GetUser()
        {
            return _context.Users.First(i => i.UserName.Equals(User.Identity.Name));
        }
        #endregion
    }
}
