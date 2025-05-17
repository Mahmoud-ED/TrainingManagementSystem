using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;
using TrainingManagementSystem.ViewModels.Identity;

namespace TrainingManagementSystem.Controllers
{
    [Authorize(Roles = "Prog")]
    [ViewLayout("_LayoutDashboard")]
    public class RolesController : BaseController
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUnitOfWork<UserProfile> _userProfile;
        private readonly IEmailSender _emailSender;
        //private readonly UserSessionTracker _userSessionTracker;
        private readonly IWebHostEnvironment _host;


        public RolesController(RoleManager<IdentityRole> roleManager,
                               UserManager<ApplicationUser> userManager,
                                SignInManager<ApplicationUser> signInManager,
                               IUnitOfWork<UserProfile> userProfile,
                                 IEmailSender emailSender,
                                 //UserSessionTracker userSessionTracker,
                                IWebHostEnvironment host) : base(host)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _userProfile = userProfile;
            _emailSender = emailSender;
            //_userSessionTracker = userSessionTracker;
            _host = host;
        }


        //[Route("Roles")]
        public async Task<IActionResult> Roles(string roleInUse)
        {
            //var users = await _userManager.Users.ToListAsync();
            //var allUsersCount = _userManager.Users.Count();
            //var usersInRole = await _userManager.GetUsersInRoleAsync("Admin");


            ViewBag.RoleInUse = roleInUse;

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var listRolesVM = new List<RolesVM>();

            var roles = await _roleManager.Roles.ToListAsync();
            foreach (var role in roles)
            {
                listRolesVM.Add(new RolesVM
                {
                    Id = role.Id,
                    Name = role.Name,
                    UsersCount = (await _userManager.GetUsersInRoleAsync(role.Name)).Count()
                });
            }

            return View(listRolesVM.OrderBy(r => r.Name));

            //if (await _userManager.IsInRoleAsync(user, "Prog"))
            //{
            //    return View(listRolesVM.OrderBy(r => r.Name));
            //}
            //else
            //{
            //    return View(listRolesVM.Where(r => r.Name != "Prog").OrderBy(r => r.Name));
            //}

        }

        public IActionResult CreateRole()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(IdentityRole identityRole)
        {
            if (!ModelState.IsValid)
            {
                return View(identityRole);
            }

            identityRole.ConcurrencyStamp = Guid.NewGuid().ToString();

            IdentityResult result = await _roleManager.CreateAsync(identityRole);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "The Role: '" + identityRole.Name + "' has been added successfully";
                return RedirectToAction(nameof(Roles));
            }
            else
            {
                TempData["ErrorMessage"] = "The Role: '" + identityRole.Name + "' failed to be added";
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(identityRole);
            }
        }
        public async Task<IActionResult> EditRole(string id)
        {
            if (id == null)
            {
                return View("NotFound");
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                ViewBag.Message = $"Cannot be found role with Id={id}";
                return View("NotFound");
            }

            var model = new EditRoleVM
            {
                Id = role.Id,
                RoleName = role.Name
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(EditRoleVM editRoleVM)
        {
            if (!ModelState.IsValid)
            {
                return View(editRoleVM);
            }

            var role = await _roleManager.FindByIdAsync(editRoleVM.Id);
            if (role == null)
            {
                ViewBag.Message = $"Cannot be found role with Id={editRoleVM.Id}";
                return View("NotFound");
            }

            role.Name = editRoleVM.RoleName;
            var result = await _roleManager.UpdateAsync(role);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "The Role: '" + role.Name + "' has been Updated successfully";
                return RedirectToAction(nameof(Roles));
            }
            else
            {
                TempData["ErrorMessage"] = "The Role: '" + role.Name + "' failed to be Updated";
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(editRoleVM);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                ViewBag.Message = $"Cannot be found role with Id={id}";
                return View("NotFound");
            }

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
            if (usersInRole.Count != 0)
            {
                TempData["ErrorMessage"] = "Deletion refused !! Because the Role: '" + role.Name + "' is in use";
                return RedirectToAction(nameof(Roles), new { roleInUse = role.Name });
            }

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "The Role: '" + role.Name + "' has been deleted successfully";
                return RedirectToAction(nameof(Roles));
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(nameof(Roles));
        }
    }
}
