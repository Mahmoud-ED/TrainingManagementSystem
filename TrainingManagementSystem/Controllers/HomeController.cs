
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Controllers;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;
using TrainingManagementSystem.ViewModels;
using TrainingManagementSystem.ViewModels.Identity;

namespace TrainingManagementSystem.Controllers
{
    //[ViewLayout("_Layout")]
    public class HomeController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork<SiteInfo> _siteInfo;
        private readonly IUnitOfWork<Contact> _contact;
        private readonly IUnitOfWork<SiteState> _siteState;
        private readonly UserSessionTracker _userSessionTracker;
        //private readonly IWebHostEnvironment _host;
        private readonly IEmailSender _emailSender;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IServiceProvider _serviceProvider;


        public HomeController(IUnitOfWork<SiteInfo> siteInfo,
                               IUnitOfWork<Contact> contact,
                               UserManager<ApplicationUser> userManager,
                               IUnitOfWork<SiteState> siteState,
                                 UserSessionTracker userSessionTracker,
                               IWebHostEnvironment host,
                               IEmailSender emailSender,
                                IServiceProvider serviceProvider,
                               SignInManager<ApplicationUser> signInManager) : base(host)
        {
            _siteInfo = siteInfo;
            _userManager = userManager;
            _contact = contact;
            _siteState = siteState;
            _userSessionTracker = userSessionTracker;
            _serviceProvider = serviceProvider;

            //_host = host;
            _emailSender = emailSender;
            _signInManager = signInManager;
        }

        [ViewLayout("_LayoutTemplate")]
        public async Task<IActionResult> Index()
        {


            //var userName = User.Identity.Name;
            //User.IsInRole("Admin");

           return View();
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



                        return RedirectToAction("Index", "Employee", new { id = user.Id });

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

        public IActionResult About()
        {

            var contact = _contact.Entity.GetAll().FirstOrDefault();

            var siteInfo = _siteInfo.Entity.GetAll().FirstOrDefault();

            SiteVM siteVM = new SiteVM
            {
                Contact = contact,
                SiteInfo = siteInfo
            };


            return View(siteVM);
        }


        [HttpPost]
        public async Task<IActionResult> Contact(string contactName, string contactEmail, string contactMessage)
        {
            string content = ReadHtmlTemplate("Contact.html");

            content = content.Replace("{SubjectName}", contactName);
            content = content.Replace("{SubjectEmail}", contactEmail);
            content = content.Replace("{Content}", contactMessage);
            string? email = await _contact.Entity.GetAll()
                                                 .Select(n => n.Email)
                                                 .FirstOrDefaultAsync();

            var message = new Message(new string[] { email }, contactEmail + " - " + contactName, content, null);

            try
            {
                await _emailSender.SendEmailAsync(message);
                TempData["SuccessMessage"] = "The email has been sent successfully";
            }
            catch
            {
                TempData["ErrorMessage"] = "Failed to send email";

                ViewBag.ErrorTitle = "Email Send Error";
                return View("Error");
            }

            return RedirectToAction("Index");
        }



        [AllowAnonymous]
        [Route("Closing")]
        public async Task<IActionResult> Closing()
        {

            var siteState = await _siteState.Entity.GetAll().FirstOrDefaultAsync();
            var contact = await _contact.Entity.GetAll().FirstOrDefaultAsync();
            var siteInfo = await _siteInfo.Entity.GetAll().FirstOrDefaultAsync();

            if (siteState == null | contact == null | siteInfo == null)
            {
                return View("NotFound");
            }

            if (siteState.State == false)
            {
                var closingVM = new ClosingVM
                {
                    SiteState = siteState,
                    Contact = contact,
                    SiteInfo = siteInfo
                };

                await _signInManager.SignOutAsync();
                // _userSessionTracker.Decrement();

                return View(closingVM);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }


    }
}

//-----طرق استدعاء الصفحات---------------------------------------
//return View(); // استدعاء نفس الصفحة بدون ارسال باراميتر
//return View(model);// استدعاء نفس الصفحة مع ارسال باراميتر مودل
//return View("Contact");//  استدعاء صفحة مختلفة بدون ارسال باراميتر
//return View("About", Id); // استدعاء صفحة مختلفة مع ارسال باراميتر عادي 
//return View("About", model); // استدعاء صفحة مختلفة مع ارسال باراميتر مودل 
//return RedirectToAction("About"); // استدعاء وظيفة
//return RedirectToAction("About" new { successMessage = successMessage}); // استدعاء وظيفة مع ارسال باراميتر