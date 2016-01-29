using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;
using _3DCytoFlow;

namespace _3DCytoFlow.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }

        public DbSet<Analysis> Analyses { get; set; }
        public DbSet<Cluster> Clusters { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<VirtualMachine> VirtualMachines { get; set; }
    }
}
