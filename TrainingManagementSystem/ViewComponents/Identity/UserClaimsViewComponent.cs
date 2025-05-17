using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.ViewModels.Identity;
using System.Security.Claims;

namespace TrainingManagementSystem.ViewComponents.Identity
{
    [ViewComponent(Name = "UserClaims")]
    public class UserClaimsViewComponent : ViewComponent
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserClaimsViewComponent(RoleManager<IdentityRole> roleManager,
                               UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"Cannot be found User with Id={userId}";
                return View("NotFound");
            }

            var userClaims = await _userManager.GetClaimsAsync(user);
            if (userClaims == null)
            {
                ViewBag.ErrorMessage = $"Cannot be found user claims";
                return View("NotFound");
            }

            var userClaimListVM = new UserClaimListVM
            {
                UserId = userId
            };



            //var userRoles = await _userManager.IsInRoleAsync(user, "Employee");

            IEnumerable<Claim>? claimsList;

            if ((await _userManager.IsInRoleAsync(user, "Employee") 
                | await _userManager.IsInRoleAsync(user, "Admin")) 
                & !await _userManager.IsInRoleAsync(user, "Prog"))
            {
                claimsList = StaticClaims.Claims.Where(n => n.Type != "EditUser" 
                                                        & n.Type != "SiteState");
            }
            else if (await _userManager.IsInRoleAsync(user, "Prog") | await _userManager.IsInRoleAsync(user, "Admin"))
            {
                claimsList = StaticClaims.Claims;
            }
            else // User/EmployeeRequest
            {
                return View(userClaimListVM);
            }

            foreach (Claim claim in claimsList)
            {
                var userClaimVM = new UserClaimVM
                {
                    ClaimType = claim.Type
                };

                if (userClaims.Any(s => s.Type == claim.Type && s.Value == "true"))
                {
                    userClaimVM.IsSelected = true;
                }
                userClaimListVM.Claims.Add(userClaimVM);
            }

            ViewBag.UserId = userId;

            return View(userClaimListVM);
        }
    }




}
