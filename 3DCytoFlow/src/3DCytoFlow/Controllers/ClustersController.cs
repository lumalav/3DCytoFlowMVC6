using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using _3DCytoFlow;
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

        [HttpPost]
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

                string strX = obj.X;
                string strY = obj.Y;
                string strZ = obj.Z;

                //get the id of the analysis
                var analysisId = int.Parse(strId);

                //create the object with all the values parsed from the model
                var cluster = new Cluster
                {
                    Name = obj.Name,
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

                return Json(true);
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

                //get the data of the dynamic model
                string strId = obj.Id;
                string strName = obj.Name;

                //parse the integer
                var analysisId = int.Parse(strId);

                //get the analysis with the clusters
                var analysis = _context.Analyses.Include(i => i.Clusters).First(i => i.Id == analysisId);

                //get cluster
                var cluster = analysis.Clusters.First(i => i.Name.Equals(strName));

                //remove the cluster
                analysis.Clusters.Remove(cluster);

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