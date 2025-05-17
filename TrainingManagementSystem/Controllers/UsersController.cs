using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrainingManagementSystem.Controllers;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;
using TrainingManagementSystem.ViewModels.Identity;
using PasswordGenerator;
using System.Security.Claims;


namespace TrainingManagementSystem.Classes
{
    //[Authorize(Roles = "Prog,Admin")]
    [Authorize(Policy = "AdminOrProgPolicy")]

    [ViewLayout("_LayoutDashboard")]
    public class UsersController : BaseController
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUnitOfWork<UserProfile> _userProfile;
        // private readonly IUnitOfWork<Employee> _employee;
        private readonly IEmailSender _emailSender;
        private readonly UserSessionTracker _userSessionTracker;
        private readonly IServiceProvider _serviceProvider;
        private readonly IWebHostEnvironment _host;


        public UsersController(RoleManager<IdentityRole> roleManager,
                               UserManager<ApplicationUser> userManager,
                               SignInManager<ApplicationUser> signInManager,
                               IUnitOfWork<UserProfile> userProfile,
                               // IUnitOfWork<Employee> employee,
                               IEmailSender emailSender,
                               UserSessionTracker userSessionTracker,
                               IServiceProvider serviceProvider,
                               IWebHostEnvironment host) : base(host)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _userProfile = userProfile;
            // _employee = employee;
            _emailSender = emailSender;
            _userSessionTracker = userSessionTracker;
            _serviceProvider = serviceProvider;
            _host = host;
        }



        public async Task<IActionResult> Users(string userList)
        {


            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            //-------------------------------------------------------------------
            var programmerSettings = _serviceProvider.GetRequiredService<IOptions<ProgrammerSettings>>().Value;

            List<ApplicationUser> users;
            if (user.UserName != programmerSettings.ProgrammerName)
            {
                users = await _userManager.Users.Include(a => a.UserProfile).Where(u => u.UserName != programmerSettings.ProgrammerName).AsNoTracking().ToListAsync();
            }
            else
            {
                users = await _userManager.Users.Include(a => a.UserProfile).AsNoTracking().ToListAsync();
            }


            ViewBag.AllUsers = users.Count();
            ViewBag.Confirmed = users.Where(u => u.EmailConfirmed == true).Count();
            ViewBag.Unconfirmed = users.Where(u => u.EmailConfirmed == false).Count();
            ViewBag.Locked = users.Where(u => u.LockoutEnd > DateTime.UtcNow).Count();
            ViewBag.ActiveUsers = _userSessionTracker.ActiveUsers;
            ViewBag.UserList = userList;

            //users = users.Where(u => u.UserName != programmerSettings.ProgrammerName).ToList();


            if (userList == "Confirmed")
            {
                return View(users.Where(u => u.EmailConfirmed == true).OrderByDescending(u => u.CreatedDate));
            }
            else if (userList == "Unconfirmed")
            {
                return View(users.Where(u => u.EmailConfirmed == false).OrderByDescending(u => u.CreatedDate));
            }
            else if (userList == "Locked")
            {
                return View(users.Where(u => u.LockoutEnd > DateTime.UtcNow).OrderByDescending(u => u.CreatedDate));
            }
            else
            {
                return View(users.OrderByDescending(u => u.CreatedDate));
            }


        }


         [Authorize(Policy = "EditUserPolicy")]
        public async Task<IActionResult> EditUser(string id)
        {
            {
                //var user1 = await _userManager.FindByIdAsync(id);
                //if (user1 == null)
                //{
                //    ViewBag.ErrorMessage = $"Cannot be found User with Id={id}";
                //    return View("NotFound");
                //}

                //string? displayName1 = await _userProfile.Entity.GetWhere(p => p.UserId == user.Id).Select(n => n.DisplayName).FirstOrDefaultAsync();

            }

            var user = await _userManager.Users.Include(p => p.UserProfile)
                                         .Where(u => u.Id == id)
                                         .FirstOrDefaultAsync();
            if (user == null)
            {
                ViewBag.ErrorMessage = $"Cannot be found User with Id={id}";
                return View("NotFound");
            }

            string? displayName = null;

            if (user.UserProfile != null)
            {
                displayName = user.UserProfile.DisplayName;
            }


            ViewBag.userId = user.Id; //ViewComponent لاستعمالها في

            var userRoles = await _userManager.GetRolesAsync(user);
            var userClaims = await _userManager.GetClaimsAsync(user);



            var editUserVM = new EditUserVM
            {
                Id = user.Id,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                LastAccessTime = user.LastAccessTime,
                Age = user.Age,
                LockoutEnd = user.LockoutEnd,
                CreatedDate = user.CreatedDate,
                ModifiedDate = user.ModifiedDate,
                Claims = userClaims.Select(m => m.Value).ToList(),
                Roles = (List<string>)userRoles,
                DisplayName = displayName,
            };

            return View(editUserVM);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserVM editUserVM)
        {
            var user = await _userManager.FindByIdAsync(editUserVM.Id);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"Cannot be found User with Id={editUserVM.Id}";
                return View("NotFound");
            }

            user.Email = editUserVM.Email;
            user.UserName = editUserVM.Email;
            user.Age = editUserVM.Age;
            user.ModifiedDate = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User data has been successfully modified";
                //return RedirectToAction(nameof(Users));
            }

            foreach (var error in result.Errors)
            {
                //ModelState.AddModelError(string.Empty, error.Description); // RedirectToAction لا تظهر لأنه لا يبقى في نفس الصفحة لأنه يقوم بتحميل الصفحة من جديد عن طريق
                TempData["ErrorMessage"] = error.Description;
            }

            return RedirectToAction("EditUser", new { id = editUserVM.Id });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "EditUserPolicy")]
        public async Task<IActionResult> LockoutUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"Cannot be found User with Id={id}";
                return View("NotFound");
            }

            var CurrentUser = await _userManager.GetUserAsync(HttpContext.User);
            if (CurrentUser.Id == id)
            {
                TempData["ErrorMessage"] = "You cannot Lockout the currently logged in user";
                return RedirectToAction(nameof(EditUser), new { id = user.Id });
            }

            var lockoutEndDate = DateTime.UtcNow.AddYears(10);

            IdentityResult result;

            if (user.LockoutEnd == null) // في حال المستخدم غير موقوف
            {
                result = await _userManager.SetLockoutEndDateAsync(user, lockoutEndDate);
            }
            else if (DateTime.UtcNow > user.LockoutEnd) // في حال المستخدم غير موقوف
            {
                result = await _userManager.SetLockoutEndDateAsync(user, lockoutEndDate);
            }
            else //موقوف
            {
                result = await _userManager.SetLockoutEndDateAsync(user, null);
            }


            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User data has been successfully modified";
            }

            foreach (var error in result.Errors)
            {
                //ModelState.AddModelError(string.Empty, error.Description); // RedirectToAction لا تظهر لأنه لا يبقى في نفس الصفحة لأنه يقوم بتحميل الصفحة من جديد عن طريق
                TempData["ErrorMessage"] = error.Description;
            }

            return RedirectToAction(nameof(EditUser), new { id = user.Id });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
         [Authorize(Policy = "EditUserPolicy")]
        public async Task<IActionResult> UserEmailConfirmed(string id, string emailConfirmed)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"Cannot be found User with Id={id}";
                return View("NotFound");
            }
            {
                //if (user.EmailConfirmed)
                //{

                //}
                //else
                //{

                //}
            }
            //------------------------------------------------------------------
            if (emailConfirmed == "Email Confirmed")
            {
                user.EmailConfirmed = true;
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "User data has been successfully modified";
                }

                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] = error.Description;
                }
            }
            //------------------------------------------------------------------
            else if (emailConfirmed == "Send confirmation link")
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action("EmailConfirm", "Account", new { userId = user.Id, token = token }, Request.Scheme);

                string content = ReadHtmlTemplate("ConfirmEmail.html");
                string subject = "Email Confirmation";
                content = content.Replace("{Subject}", subject);
                content = content.Replace("{UserName}", user.Email);
                content = content.Replace("{confirmationLink}", confirmationLink);

                var message = new Message(new string[] { user.Email }, subject, content, null);

                try
                {
                    await _emailSender.SendEmailAsync(message);
                    TempData["SuccessMessage"] = "The confirmation Link has been sent to the email successfully";
                }
                catch
                {
                    TempData["ErrorMessage"] = "Failed to send email";
                }
            }
            //------------------------------------------------------------------

            return RedirectToAction(nameof(EditUser), new { id = user.Id });
        }


        [HttpPost]
        //[Authorize(Roles = "Prog")]
        [Authorize(Policy = "EditUserPolicy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"Cannot be found User with Id={id}";
                return View("NotFound");
            }

            var CurrentUser = await _userManager.GetUserAsync(HttpContext.User);
            if (CurrentUser.Id == id)
            {
                TempData["ErrorMessage"] = "You cannot delete the currently logged in user";
                return RedirectToAction(nameof(EditUser), new { id = user.Id });
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "The user has been deleted successfully";
                return RedirectToAction(nameof(Users));
            }


            foreach (var error in result.Errors)
            {
                //ModelState.AddModelError(string.Empty, error.Description);
                TempData["ErrorMessage"] = error.Description;
            }

            return RedirectToAction(nameof(EditUser), new { id = user.Id });

        }


        public async Task<IActionResult> CreateUser()
        {
            //-----AllowAnnonymous- الا في حال الوظائف التي خصصنا لها-Authorize لا داعي لمثل هذه الشروط بسبب وجود---------------------
            {
                //if (_signInManager.IsSignedIn(User) & !User.IsInRole("Admin") & !User.IsInRole("Prog"))
                //{
                //    return RedirectToAction("Home", "Index");
                //}
            }
            //-----------------------------------------------------------------------------------------

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return View("NotFound");
            }

            List<IdentityRole> roles;

            //if (await _userManager.IsInRoleAsync(user, "Prog"))
            //{
            //    roles = await _roleManager.Roles.Where(r => r.Name != "Employee" & r.Name != "EmployeeRequest").OrderBy(r => r.Name).ToListAsync();
            //}
            //else
            //{
            //    roles = await _roleManager.Roles.Where(r => r.Name != "Prog" & r.Name != "Employee" & r.Name != "EmployeeRequest").OrderBy(r => r.Name).ToListAsync();
            //}

            roles = await _roleManager.Roles.ToListAsync(); 
            ViewData["Roles"] = new SelectList(roles, "Id", "Name");

            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserVM createUserVM)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = createUserVM.Email,
                    Email = createUserVM.Email,
                    Age = createUserVM.Age,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {

                    
                    //var password1 = new Password();
                    var password = new Password(true, true, true, true, 5);
                    string generatedPassword = password.Next();

                    var hasher = new PasswordHasher<ApplicationUser>();
                    user.PasswordHash = hasher.HashPassword(user, generatedPassword);


                    var role = await _roleManager.Roles.Where(r => r.Id == createUserVM.RoleId)
                                                       .FirstOrDefaultAsync();
                    if (role == null)
                    {
                        TempData["ErrorMessage"] = "Cannot be found Role with Id : '" + createUserVM.RoleId + "'";
                        return RedirectToAction("Roles", "Roles");
                    }

                    if (!(await _userManager.AddToRoleAsync(user, role.Name)).Succeeded)
                    {
                        TempData["ErrorMessage"] = "Failed to add user to Role: '" + role.Name + "'";
                        return RedirectToAction("Roles", "Roles");
                    }


                    //------------------------Send email--------------------------------------------
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationLink = Url.Action("EmailConfirm", "Account", new { userId = user.Id, token = token }, Request.Scheme);

                    string content = ReadHtmlTemplate("ConfirmEmailWithPassword.html");
                    string subject = "Email Confirmation";
                    content = content.Replace("{Subject}", subject);
                    content = content.Replace("{UserName}", createUserVM.Email);
                    content = content.Replace("{Password}", generatedPassword);
                    content = content.Replace("{confirmationLink}", confirmationLink);

                    var message = new Message(new string[] { createUserVM.Email }, subject, content, null);

                    try
                    {
                        await _emailSender.SendEmailAsync(message);
                        TempData["SuccessMessage"] = "The confirmation Link has been sent to the email successfully";
                    }
                    catch
                    {
                        TempData["ErrorMessage"] = "Failed to send email";
                    }
                    return RedirectToAction(nameof(Users));
                    //----------------------------------------------------------------------------
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(createUserVM);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "EditUserPolicy")]
        public async Task<IActionResult> EditUserRoles(List<UserRolesVM> model, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"Cannot be found user with Id={userId}";
                return View("NotFound");
            }

            var roles = await _userManager.GetRolesAsync(user);
            if ((await _userManager.RemoveFromRolesAsync(user, roles)).Succeeded)//var result = await _userManager.RemoveFromRoleAsync(user, "EmployeeRequest"); // لحذف صلاحية واحدة فقط
            {
                if ((await _userManager.AddToRolesAsync(user, model.Where(s => s.IsSelected).Select(r => r.RoleName))).Succeeded)
                {
                    TempData["SuccessMessage"] = "User roles have been modified successfully";
                    return RedirectToAction("EditUser", new { Id = userId });


                    //SiteState - EditUser//اختبار هل اليوزر ليس ادمن او مبرمج يتم سحب الصلاحيتين لأنهم سيختفو ويبقو مفعلين

                }
            }

            TempData["ErrorMessage"] = "Failed to modify user Roles";
            ModelState.AddModelError(string.Empty, "Failed to modify user Roles");

            return RedirectToAction("EditUser", new { Id = userId });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "EditUserPolicy")]
        public async Task<IActionResult> EditUserClaims(UserClaimListVM userClaimListVM, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"Cannot be found user with Id={userId}";
                return View("NotFound");
            }

            var claims = await _userManager.GetClaimsAsync(user);
            var result = await _userManager.RemoveClaimsAsync(user, claims); //var result = await _userManager.RemoveFromClaimAsync(user, "Edit"); // لسحب مطالبة واحدة فقط
            if (result.Succeeded)
            {
                result = await _userManager.AddClaimsAsync(user, userClaimListVM.Claims
                         .Select(c => new Claim(c.ClaimType, c.IsSelected ? "true" : "false")));
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "User Claims have been modified successfully";
                    return RedirectToAction("EditUser", new { Id = userId });
                }
            }
            TempData["ErrorMessage"] = "Failed to modify user Claims";
            return RedirectToAction("EditUser", new { Id = userId });
        }






    }
}

