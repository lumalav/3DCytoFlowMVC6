using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using _3DCytoFlow;
using _3DCytoFlow.Models;

namespace _3DCytoFlow.Controllers
{
    [Produces("application/json")]
    [Route("api/Clusters")]
    public class ClustersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClustersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Clusters
        [HttpGet]
        public IEnumerable<Cluster> GetCluster()
        {
            return _context.Clusters;
        }

        // GET: api/Clusters/5
        [HttpGet("{id}", Name = "GetCluster")]
        public async Task<IActionResult> GetCluster([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return HttpBadRequest(ModelState);
            }

            Cluster cluster = await _context.Clusters.SingleAsync(m => m.Id == id);

            if (cluster == null)
            {
                return HttpNotFound();
            }

            return Ok(cluster);
        }

        // PUT: api/Clusters/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCluster([FromRoute] int id, [FromBody] Cluster cluster)
        {
            if (!ModelState.IsValid)
            {
                return HttpBadRequest(ModelState);
            }

            if (id != cluster.Id)
            {
                return HttpBadRequest();
            }

            _context.Entry(cluster).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClusterExists(id))
                {
                    return HttpNotFound();
                }
                else
                {
                    throw;
                }
            }

            return new HttpStatusCodeResult(StatusCodes.Status204NoContent);
        }

        // POST: api/Clusters
        [HttpPost]
        public async Task<IActionResult> PostCluster([FromBody] Cluster cluster)
        {
            if (!ModelState.IsValid)
            {
                return HttpBadRequest(ModelState);
            }

            _context.Clusters.Add(cluster);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (ClusterExists(cluster.Id))
                {
                    return new HttpStatusCodeResult(StatusCodes.Status409Conflict);
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("GetCluster", new { id = cluster.Id }, cluster);
        }

        // DELETE: api/Clusters/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCluster([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return HttpBadRequest(ModelState);
            }

            Cluster cluster = await _context.Clusters.SingleAsync(m => m.Id == id);
            if (cluster == null)
            {
                return HttpNotFound();
            }

            _context.Clusters.Remove(cluster);
            await _context.SaveChangesAsync();

            return Ok(cluster);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool ClusterExists(int id)
        {
            return _context.Clusters.Count(e => e.Id == id) > 0;
        }
    }
}