using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;
using _3DCytoFlow.ViewModels.VirtualMachine;

namespace _3DCytoFlow.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Analysis> Analyses { get; set; }
        public DbSet<Cluster> Clusters { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<VirtualMachine> VirtualMachines { get; set; }
        public DbSet<DetailsViewModel> DetailsViewModel { get; set; }
    }
}
