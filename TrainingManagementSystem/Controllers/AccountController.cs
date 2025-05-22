
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System.Security.Claims;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Controllers;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;
using TrainingManagementSystem.ViewModels.Identity;

namespace TrainingManagementSystem.Controllers
{
    //[AllowAnonymous]  لآكشن معينة داخل الكونترولر Authorize عند استعماله لايمكن تفعيل

    [ViewLayout("_Layout")]
    public class AccountController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _host;
        private readonly IEmailSender _emailSender;
        private readonly UserSessionTracker _userSessionTracker;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork<Employee> _employee;
        private readonly IUnitOfWork<UserProfile> _userProfile;
        private readonly IServiceProvider _serviceProvider;

        public AccountController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 IWebHostEnvironment host,
                                 IEmailSender emailSender,
                                 UserSessionTracker userSessionTracker,
                                 RoleManager<IdentityRole> roleManager,
                                  IUnitOfWork<Employee> employee,
                                 IUnitOfWork<UserProfile> userProfile,
                                  IServiceProvider serviceProvider) : base(host)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _host = host;
            _emailSender = emailSender;
            _userSessionTracker = userSessionTracker;
            _roleManager = roleManager;
            _employee = employee;
            _userProfile = userProfile;
            _serviceProvider = serviceProvider;
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM registerVM)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = registerVM.Email,
                    Email = registerVM.Email,
                    //LockoutEnabled=false, //Default عكس 
                    //EmailConfirmed = true,  //Default عكس 
                    Age = registerVM.Age,
                    CreatedDate = DateTime.UtcNow
                };

                var result =
                    await _userManager.CreateAsync(user, registerVM.Password);

                if (result.Succeeded)
                {
                    {
                        //------------------------Send email--------------------------------------------
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var confirmationLink = Url.Action("EmailConfirm", "Account", new { userId = user.Id, token = token }, Request.Scheme);

                        string content = ReadHtmlTemplate("ConfirmEmail.html");
                        string subject = "Email Confirmation";
                        content = content.Replace("{Subject}", subject);
                        content = content.Replace("{UserName}", registerVM.Email);
                        content = content.Replace("{confirmationLink}", confirmationLink);

                        var message = new Message(new string[] { registerVM.Email }, subject, content, null);

                        try
                        {
                            await _emailSender.SendEmailAsync(message);
                            TempData["SuccessMessage"] = "The email has been sent successfully";
                        }
                        catch
                        {
                            TempData["ErrorMessage"] = "Failed to send email";
                            return View(registerVM);
                        }

                        //---------------------------------------------------------------------------------
                    }

                    ViewBag.SuccessTitle = "Email confirmation is required";
                    ViewBag.SuccessMessage = "Please check your email, we sent confirmation link";
                    return View("Result");
                }

                foreach (var error in result.Errors)//  summary قائمة الأخطاء التي تظهر في
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(registerVM);
        }
        [AcceptVerbs("Get", "Post")]
        public async Task<IActionResult> IsEmailInUse(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Json(true);
            }
            else
            {
                return Json($"The User Name:'{email}' is already in use");
            }
        }
        public async Task<IActionResult> EmailConfirm(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return View(nameof(NotFound));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"Cannot be found user with Id={userId}";
                return View(nameof(NotFound));
            }

            if (user.EmailConfirmed)
            {
                ViewBag.ErrorMessage = "You have already verified your email address";
                return View();
            }

            if ((await _userManager.ConfirmEmailAsync(user, token)).Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Count == 0)
                {
                    //-------------------------Add Role User------------------------------------
                    if (!await _roleManager.RoleExistsAsync("User"))
                    {
                        var userRole = new IdentityRole
                        {
                            Name = "User",
                            ConcurrencyStamp = Guid.NewGuid().ToString()
                        };

                        if (!(await _roleManager.CreateAsync(userRole)).Succeeded)
                        {
                            ViewBag.ErrorMessage = "The Role: '" + userRole.Name + "' failed to be added";
                            return View();
                        }
                    }

                    //-------------------------Add user to Role User---------------------------
                    if (!(await _userManager.AddToRoleAsync(user, "User")).Succeeded)
                    {
                        ViewBag.ErrorMessage = "Cannot add Roles to user";
                        return View();
                    }
                    //--------------------------------------------------------------------------
                }


                //------------------------Employee Message----------------------------------
                bool isEmployee = await _userManager.IsInRoleAsync(user, "EmployeeRequest");
                if (isEmployee)
                {
                    ViewBag.Message = "You need the << admin's approval >> to activate your account 'as an employee'";
                    return View();
                }
                //------------------------------------------------------------------------


                return View();
            }

            ViewBag.ErrorMessage = "Email Confirmation failed";
            return View();
        }
        public Task<IActionResult> Login()
        {
            if (_signInManager.IsSignedIn(User))
            {
                return Task.FromResult<IActionResult>(RedirectToAction("Register", "Account"));
            }

            var loginViewModel = new LoginVM
            {
                Email = string.Empty,    // تهيئة الحقول المطلوبة
                Password = string.Empty, // تهيئة الحقول المطلوبة
                RememberMe = false       // القيمة الافتراضية لـ bool
            };

            return Task.FromResult<IActionResult>(View(loginViewModel)); 


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(loginVM.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Email Not Found");
                    return View(loginVM);
                }

                if (!await _userManager.IsEmailConfirmedAsync(user)
                    && await _userManager.CheckPasswordAsync(user, loginVM.Password)) // بحيث انه لن يعطي رسالة الكونفيرم الا اذا كان الاسم والباسوورد صحيحين
                {
                    ViewBag.ErrorTitle = "Login failed";
                    ViewBag.ErrorMessage = "You must have a confirmed email to log on.";
                    return View("Result");
                }



                //var result1 = await _signInManager.PasswordSignInAsync(user, loginVM.Password, loginVM.RememberMe, true);

                var result = await _signInManager.PasswordSignInAsync(loginVM.Email, loginVM.Password, loginVM.RememberMe, true);
                if (result.Succeeded)
                {
                    //ViewBag.userid = user.Id;
                    user.LastAccessTime = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                    _userSessionTracker.Increment();

                    //string existingRole = _userManager.GetRolesAsync(user).Result.Single();
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    else if (await _userManager.IsInRoleAsync(user, "Prog"))
                    {
                        //return RedirectToAction("Index", "Admin");

                        //----------------------------------
                        {
                            var programmerSettings = _serviceProvider.GetRequiredService<IOptions<ProgrammerSettings>>().Value;
                            if (user.UserName == programmerSettings.ProgrammerName)
                            {
                                if (await CheckProgClaims(user, loginVM))
                                {
                                    return RedirectToAction("Index", "Admin");
                                }
                            }

                            return RedirectToAction("Index", "Admin");
                        }
                        //----------------------------------------------------------------
                    }
                    else
                    {
                        //Guid EmployeeId = await _employee.Entity.GetWhere(n=>n.UserId== user.Id).Select(n=>n.Id ).FirstOrDefaultAsync() ;   
                        //return RedirectToAction("Index", "Employee" ,EmployeeId);

                      
                      
                            return RedirectToAction("Index", "Employee",  new { id = user.Id });
                       
                    }
                }

                if (result.IsLockedOut)
                {
                    if (user.LockoutEnd.Value.Year > DateTime.Now.Year)
                    {
                        ViewBag.ErrorTitle = "Login failed";
                        ViewBag.ErrorMessage = $"The user '{loginVM.Email}' is locke out from Administrator.";
                        return View("Result");
                    }
                    else
                    {
                        ViewBag.ErrorTitle = "Login failed";
                        ViewBag.ErrorMessage = $"You made 5 wrong attempts to enter the password ,the user '{loginVM.Email}' is locked now, please try again afte 15 Minutes";
                        ViewBag.ResetPassword = "Reset Password";
                        return View("Result");
                    }

                }

                ModelState.AddModelError(string.Empty, "Invalid Login Attempt");
            }
            return View(loginVM);
        }

        public async Task<bool> CheckProgClaims(ApplicationUser user, LoginVM model)
        {

            var claims = await _userManager.GetClaimsAsync(user);

            if (claims.Count < StaticClaims.Claims.Count) //عند اول دخول للمبرمج يكون العدد صفر
            {
                if ((await _userManager.RemoveClaimsAsync(user, claims)).Succeeded)
                {
                    if ((await _userManager.AddClaimsAsync(user, StaticClaims.Claims)).Succeeded)
                    {
                        return true;
                    }
                }
                return false;
            }
            else // في حال قام أحد المبرمجين بسحب الصلاحيات من المبرمج الرئيسي 
            {
                foreach (var claim in claims)
                {
                    //----------------------لتخصيص جميع الصلاحيات-----------------------------
                    if (claim.Value == "false")
                    {
                        if (!(await _userManager.ReplaceClaimAsync(user, claim, new Claim(claim.Type, "true"))).Succeeded)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }


        [Authorize]
        public async Task<IActionResult> UserProfile(string userId)
        {
            if (userId == null)
            {
                return View("NotFound");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View("NotFound");
            }

            ViewBag.Email = user.Email;

            //  var userProfile = await _userProfile.Entity.GetWhere(p => p.UserId == userId).Include(u => u.ApplicationUser).FirstOrDefaultAsync(); //  يمكن تضمين المستخدم لكن في حال يوجد بروفايل له اي في التعديل وليس في الاضافة

            var userProfile = await _userProfile.Entity.GetWhere(p => p.UserId == userId).FirstOrDefaultAsync();

            return View(userProfile);

        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _userSessionTracker.Decrement();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserProfile(UserProfile userProfile, string gender, string? isImg1)
        {
            var displayName = await _userProfile.Entity
                             .GetWhere(u => u.DisplayName == userProfile.DisplayName
                             & u.Id != userProfile.Id)
                             .Select(n => n.DisplayName)
                             .FirstOrDefaultAsync();

            if (displayName != null)
            {
                TempData["ErrorMessage"] = "The display name is reserved";
                return View(userProfile);
            }

            string? fileName = UploadFile("UserProfile", userProfile.Image, userProfile.ImageUrl, isImg1);

            userProfile.ImageUrl = fileName;


            if (userProfile.Id == Guid.Empty)
            {
                userProfile.Created = DateTime.UtcNow;

                if (gender == "Male")
                    userProfile.Gender = true;
                else
                    userProfile.Gender = false;

                try
                {

                    _userProfile.Entity.Insert(userProfile);
                    await _userProfile.SaveAsync();
                }
                catch
                {
                    return View("Error");
                }
            }

            else
            {
                userProfile.Modified = DateTime.UtcNow;

                try
                {
                    _userProfile.Entity.Update(userProfile);
                    await _userProfile.SaveAsync();
                }
                catch
                {
                    return View("Error");
                }
            }

            TempData["SuccessMessage"] = "Saved successfully";



            return RedirectToAction("UserProfile", new { userId = userProfile.UserId });
        }

        [AcceptVerbs("Get", "Post")]
        public async Task<IActionResult> IsDisplayNameInUse(string displayName, Guid id)
        {
            var name = await _userProfile.Entity
                             .GetWhere(u => u.DisplayName == displayName
                              & u.Id != id)
                             .Select(n => n.DisplayName).FirstOrDefaultAsync();
            if (name == null)
            {
                return Json(true);
            }
            else
            {
                return Json($"The display name:'{name}' is already in use");
            }
        }

        public IActionResult ForgotPassword()
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM forgotPasswordVM)
        {
            if (!ModelState.IsValid)
            {
                return View(forgotPasswordVM);
            }

            var user = await _userManager.FindByEmailAsync(forgotPasswordVM.Email);
            if (user != null)
            {
                if (await _userManager.IsEmailConfirmedAsync(user))
                {

                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResetLink = Url.Action("ResetPassword", "Account", new { email = forgotPasswordVM.Email, token = token }, Request.Scheme);
                    //------------------send email-----------------------------------


                    string content = ReadHtmlTemplate("ForgotPassword.html");

                    var subject = "Forgot password";
                    content = content.Replace("{Subject}", subject);
                    content = content.Replace("{UserName}", forgotPasswordVM.Email);
                    content = content.Replace("{passwordResetLink}", passwordResetLink);

                    var message = new Message(new string[] { forgotPasswordVM.Email }, subject, content, null);
                    try
                    {
                        await _emailSender.SendEmailAsync(message);
                    }
                    catch
                    {
                        ViewBag.ErrorTitle = "Reset password Error";
                        ViewBag.ErrorMessage = "Failed to send email";
                        return View("Error");
                    }
                    //-------------------------------------------------------
                    ViewBag.SuccessTitle = "Reset password";
                    ViewBag.SuccessMessage = "Please check your email, The link of reset password has been sent to your email";
                    return View("Result");
                }

                ViewBag.ErrorTitle = "Failed Reset password";
                ViewBag.ErrorMessage = "Your account needs to be Email Confirmation";
                return View("Result");
            }

            ViewBag.ErrorTitle = "Failed Reset password";
            ViewBag.ErrorMessage = "We cannot find the email, please check your email spelling ";
            return View("Result");
        }
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid password reset link");
            }

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM resetPasswordVM)
        {
            if (!ModelState.IsValid)
            {
                return View(resetPasswordVM);
            }

            var user = await _userManager.FindByEmailAsync(resetPasswordVM.Email);
            if (user != null)
            {
                var result = await _userManager.ResetPasswordAsync(user, resetPasswordVM.Token, resetPasswordVM.Password);
                if (result.Succeeded)
                {
                    ViewBag.SuccessTitle = "Reset password";
                    ViewBag.SuccessMessage = "Reset password successfully";
                    return View("Result");
                }

                //-------------------للمقارنة-------------------------------
                {
                    //foreach (var error1 in result.Errors)//  summary قائمة الأخطاء التي تظهر في
                    //{
                    //    ModelState.AddModelError(string.Empty, error1.Description);
                    //}

                    //ModelState.AddModelError(string.Empty, "Invalid Login Attempt");
                }
                //---------------------------------------------------------

                var error = result.Errors.First();
                ModelState.AddModelError(string.Empty, error.Description);

                return View(resetPasswordVM);
            }

            ViewBag.ErrorTitle = "Failed Reset password";
            ViewBag.ErrorMessage = "Your account not found";
            return View("Result");
        }

        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);

                ViewBag.SuccessTitle = "Change password";
                ViewBag.SuccessMessage = "Change password successfully";
                return View("Result");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View();
        }

        public IActionResult AccessDenied(string message)
        {
            ViewBag.Message = message;
            return View();
        }



        public IActionResult EmployeeRegister()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmployeeRegister(EmployeeRegisterVM employeeRegisterVM)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = employeeRegisterVM.RegisterVM.Email,
                    Email = employeeRegisterVM.RegisterVM.Email,
                    Age = employeeRegisterVM.RegisterVM.Age,
                    Approval = false,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, employeeRegisterVM.RegisterVM.Password);
                if (result.Succeeded)
                {
                    //-------------------------Add Employee row------------------------------------
                    var userId = user.Id;
                    var employee = new Employee
                    {
                        Name = employeeRegisterVM.Employee.Name,
                        PhoneNumber = employeeRegisterVM.Employee.PhoneNumber,
                        Address = employeeRegisterVM.Employee.Address,
                        YearsOfExperience = employeeRegisterVM.Employee.YearsOfExperience,
                        Specialization = employeeRegisterVM.Employee.Specialization,
                        Bio = employeeRegisterVM.Employee.Bio,
                        UserId = userId,
                        Created = DateTime.UtcNow
                    };

                    try
                    {
                        _employee.Entity.Insert(employee);
                        await _employee.SaveAsync();
                    }
                    catch
                    {
                        return View("Error");
                    }

                    //-------------------------Add Role EmployeeRequest------------------------------------
                    if (!await _roleManager.RoleExistsAsync("EmployeeRequest"))
                    {
                        var employeeRequestRole = new IdentityRole
                        {
                            Name = "EmployeeRequest",
                            ConcurrencyStamp = Guid.NewGuid().ToString()
                        };

                        if (!(await _roleManager.CreateAsync(employeeRequestRole)).Succeeded)
                        {
                            TempData["ErrorMessage"] = "The Role: '" + employeeRequestRole.Name + "' failed to be added";
                            return View("Error");
                        }
                    }

                    //-----------------------Add User To Role EmployeeRequest-------------------------------
                    if (!(await _userManager.AddToRoleAsync(user, "EmployeeRequest")).Succeeded)
                    {
                        TempData["ErrorMessage"] = "Failed to add user to Role: 'EmployeeRequest'";
                        return View("Error");
                    }

                    //------------------------Send email---------------------------------------------
                    {
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var confirmationLink = Url.Action("EmailConfirm", "Account", new { userId = user.Id, token = token }, Request.Scheme);

                        string content = ReadHtmlTemplate("ConfirmEmail.html");
                        string subject = "Email Confirmation";
                        content = content.Replace("{Subject}", subject);
                        content = content.Replace("{UserName}", employeeRegisterVM.RegisterVM.Email);
                        content = content.Replace("{Password}", employeeRegisterVM.RegisterVM.Password);
                        content = content.Replace("{confirmationLink}", confirmationLink);

                        var message = new Message(new string[] { employeeRegisterVM.RegisterVM.Email }, subject, content, null);

                        try
                        {
                            await _emailSender.SendEmailAsync(message);
                            TempData["SuccessMessage"] = "The email has been sent successfully";
                        }
                        catch
                        {
                            TempData["ErrorMessage"] = "Failed to send email";
                            return View(employeeRegisterVM);
                        }
                    }
                    //---------------------------------------------------------------------------------

                    ViewBag.SuccessTitle = "Email confirmation required";
                    ViewBag.SuccessMessage = "Please check your email, we sent confirmation link";
                    return View("Result");

                    //await _signInManager.SignInAsync(user, isPersistent: false); //isPersistent:false // Session cockie تنتهي عند اغلاق المتصفح مباشرة 
                    //return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(employeeRegisterVM);
        }


        [Authorize]
        public async Task<IActionResult> EmployeeProfile(string? userId)
        {
            if (userId == null)
            {
                return View("NotFound");
            }

            var employee = await _employee.Entity.GetWhere(e => e.UserId.ToString() == userId)
                                                 .Include(u => u.ApplicationUser)
                                                 .FirstOrDefaultAsync();
            if (employee == null)
            {
                ViewBag.ErrorMessage = $"Cannot be found employee with Id={userId}";
                return View("NotFound");
            }

            return View(employee);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmployeeProfile([Bind("Id,Name,PhoneNumber,Address,YearsOfExperience,Specialization,Bio,UserId,Created")] Employee employee)
        {
            //-----------------------------------------
            var applicationUser = await _userManager.FindByIdAsync(employee.UserId);
            if (applicationUser == null)
            {
                ViewBag.ErrorMessage = $"Cannot be found User with Id={employee.UserId}";
                return View("NotFound");
            }
            employee.ApplicationUser = applicationUser;
            //--------------------------------------------------------------------

            if (!ModelState.IsValid)
            {
                return View(employee);
            }

            var name = await _employee.Entity.GetWhere(e => e.Id != employee.Id & e.Name == employee.Name).Select(e => e.Name).FirstOrDefaultAsync();
            if (name != null)
            {
                TempData["ErrorMessage"] = "The Employee name is reserved";
                return View(employee);
            }

            try
            {
                employee.Modified = DateTime.UtcNow;
                _employee.Entity.Update(employee);
                await _employee.SaveAsync();
            }
            catch (Exception)
            {
                return View("Error");
            }

            TempData["SuccessMessage"] = "Saved successfully";
            return RedirectToAction("EmployeeProfile", new { employee.UserId });
        }


        //[HttpPost]
        //[Authorize(Roles = "Admin")] // إذا تحب تقصرها على المشرفين فقط
        //public async Task<IActionResult> ChangeUserPassword(string userId, string newPassword)
        //{
        //    if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(newPassword))
        //        return BadRequest("Invalid user ID or password.");

        //    var user = await _userManager.FindByIdAsync(userId);

        //    if (user == null)
        //        return NotFound("User not found.");

        //    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        //    var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        //    if (result.Succeeded)
        //    {
        //        return Ok("Password changed successfully.");
        //    }

        //    return BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));
        //}
        [HttpPost]
        public async Task<IActionResult> ChangeUserPassword(string id, string NewPassword)
        {
            if (string.IsNullOrWhiteSpace(id) )
                return BadRequest("User ID or new password is missing.");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound("User not found.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, "P111");

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password changed successfully.";

                return View();
            }

            TempData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));

            return View();
        }


    }
}
