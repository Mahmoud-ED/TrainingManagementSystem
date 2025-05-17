

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.ViewModels.Identity;

namespace TrainingManagementSystem.ViewComponents.Identity
{

    [ViewComponent(Name = "UserRoles")]
    public class UserRolesViewComponent : ViewComponent
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserRolesViewComponent(RoleManager<IdentityRole> roleManager,
                               UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(string? userId)
        {
            if (userId == null)
            {
                return View("NotFound");  
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"Cannot be found User with Id={userId}";
                return View("NotFound");
            }

            var userRolesListVM = new List<UserRolesVM>();

            foreach (var role in _roleManager.Roles.OrderBy(r => r.Name).ToList())
            {
                var userRolesVM = new UserRolesVM
                {
                    RoleId = role.Id,
                    RoleName = role.Name
                };

                if (await _userManager.IsInRoleAsync(user, role.Name))
                {
                    userRolesVM.IsSelected = true;
                }
                else
                {
                    userRolesVM.IsSelected = false;
                }
                userRolesListVM.Add(userRolesVM);
            }

            ViewBag.UserId = userId;

            return View(userRolesListVM);

        }

    }


}
