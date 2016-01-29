using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using _3DCytoFlow.Models;

namespace _3DCytoFlow.Services
{
    public class DbSeeder : ISeeder
    {
        public ApplicationDbContext Context { get; }
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;


        public DbSeeder(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            Context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }


        public async void EnsureSeedData()
        {
            bool adminRoleExists = await _roleManager.RoleExistsAsync("Admin");
            bool doctorRoleExists = await _roleManager.RoleExistsAsync("Doctor");

            // Add roles: ADMIN DOCTOR
            if ( !adminRoleExists) await _roleManager.CreateAsync(new IdentityRole { Name = "Admin" });
            if ( !doctorRoleExists) await _roleManager.CreateAsync(new IdentityRole { Name = "Doctor" });

            ApplicationUser luis = new ApplicationUser
            {
                UserName = "lumalav@gmail.com",
                FirstName = "Luis",
                LastName = "Lavieri",
                Email = "lumalav@gmail.com"
            };
            ApplicationUser manuel = new ApplicationUser
            {
                UserName = "manogonzalez1994@gmail.com",
                FirstName = "Manuel",
                LastName = "Gonzalez",
                Email = "manogonzalez1994@gmail.com"
            };
            ApplicationUser ryan = new ApplicationUser
            {
                UserName = "ryangonyon@knights.ucf.edu",
                FirstName = "Ryan",
                LastName = "Gonyon",
                Email = "ryangonyon@knights.ucf.edu"
            };

            bool luisExists = _userManager.Users.Any(x => x.UserName == "lumalav@gmail.com");
            bool manuelExists = _userManager.Users.Any(x => x.UserName == "manogonzalez1994@gmail.com");
            bool ryanExists = _userManager.Users.Any(x => x.UserName == "ryangonyon@knights.ucf.edu");

            // Add luis, manuel and ryan if they don't exists
            if( !luisExists ) await _userManager.CreateAsync(luis, "Hello1234*");
            if (!manuelExists) await _userManager.CreateAsync(manuel, "Hello1234*");
            if (!ryanExists) await _userManager.CreateAsync(ryan, "Hello1234*");

            luis = _userManager.Users.First(x => x.UserName == "lumalav@gmail.com");
            manuel = _userManager.Users.First(x => x.UserName == "manogonzalez1994@gmail.com");
            ryan = _userManager.Users.First(x => x.UserName == "ryangonyon@knights.ucf.edu");

            bool luisAsAdmin = await _userManager.IsInRoleAsync(luis, "Admin");
            bool manuelAsAdmin = await _userManager.IsInRoleAsync(manuel, "Admin");
            bool ryanAsAdmin = await _userManager.IsInRoleAsync(ryan, "Admin");

            if (!luisAsAdmin) await _userManager.AddToRoleAsync(luis, "Admin");
            if (!manuelAsAdmin) await _userManager.AddToRoleAsync(manuel, "Admin");
            if (!ryanAsAdmin) await _userManager.AddToRoleAsync(ryan, "Admin");
        }
    }

    public interface ISeeder
    {
        void EnsureSeedData();
    }
}
