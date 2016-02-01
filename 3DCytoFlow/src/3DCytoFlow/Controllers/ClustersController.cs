using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using _3DCytoFlow.Models;

namespace _3DCytoFlow.Controllers
{
    public class ClustersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClustersController(ApplicationDbContext context)
        {
            _context = context;    
        }
        //TODO: Convert model to dynamic object or save the model as ClusterModel from client 
        [HttpPost]
        public async Task<ActionResult> SaveCluster(string analysisId, string model)
        {
            if (User.Identity.IsAuthenticated)
            {
                var analysis = _context.Analyses.Single(i => i.Id == int.Parse(analysisId));
            }
            return Content("");
        }
    }
}
