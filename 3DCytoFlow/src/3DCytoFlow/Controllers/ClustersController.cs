using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using Newtonsoft.Json;
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

        [HttpGet]
        public IActionResult SaveCluster(string model)
        {
            if (User.Identity.IsAuthenticated)
            {
                //deserialize object model
                dynamic obj = JsonConvert.DeserializeObject(model);

                //get the data of the dynamic model
                //it has to be this way or throws binding exception
                string strId = obj.Id;
                string strDepth = obj.Depth;
                string strHeight = obj.Height;
                string strWidth = obj.Width;
                string strColor = obj.Color;

                string strX = obj.X;
                string strY = obj.Y;
                string strZ = obj.Z;

                //get the id of the analysis
                var analysisId = int.Parse(strId);

                //create the object with all the values parsed from the model
                var cluster = new Cluster
                {
                    Name = obj.Name + "#" + strColor,
                    Depth = double.Parse(strDepth),
                    Height = double.Parse(strHeight),
                    Width = double.Parse(strWidth),
                    X = double.Parse(strX),
                    Y = double.Parse(strY),
                    Z = double.Parse(strZ)
                };

                //get the analysis
                var analysis = _context.Analyses.First(i => i.Id == analysisId);

                //save the cluster
                analysis.Clusters.Add(cluster);

                //save the changes
                _context.SaveChanges();

                var newCluster =
                    _context.Clusters.Include(i => i.Analysis)
                        .FirstOrDefault(i => i.Analysis.Id == analysis.Id && i.Name.Equals(cluster.Name));

                if(newCluster == null) return Json(false);

                return Json(JsonConvert.SerializeObject(new { newCluster.Id }));
            }

            return RedirectToAction("LogIn", "Account");
        }

        [HttpPost]
        public IActionResult RemoveCluster(string model)
        {
            if (User.Identity.IsAuthenticated)
            {
                //deserialize object model
                dynamic obj = JsonConvert.DeserializeObject(model);

                foreach (var jsonObj in obj)
                {
                    var id = (string) jsonObj.Id;

                    //get cluster
                    var cluster = _context.Clusters.First(i => i.Id == int.Parse(id));

                    //remove the cluster
                    _context.Clusters.Remove(cluster);
                }
                
                //save the changes
                _context.SaveChanges();

                return Json(true);
            }

            return RedirectToAction("LogIn", "Account");
        }

        [HttpGet]
        public IActionResult GetClusters(string id)
        {
            if (User.Identity.IsAuthenticated)
            {
                //get the id of the analysis
                var analysisId = int.Parse(id);

                //get the analysis
                var analysis = _context.Analyses.Include(i => i.Clusters).First(i => i.Id == analysisId);

                //get the clusters
                var clusters = analysis.Clusters.ToList();

                //serialize the object
                var json = JsonConvert.SerializeObject(clusters);

                //return the object
                return Json(json);
            }

            return RedirectToAction("LogIn", "Account");
        }
    }
}