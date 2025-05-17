using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Controllers;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;
using TrainingManagementSystem.ViewModels;
using TrainingManagementSystem.ViewModels.Identity;

namespace TrainingManagementSystem.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    [Authorize(Roles = "Admin,Prog")]
    public class MailController : BaseController
    {
        private readonly MailSettings _mailSettings;
        private readonly IWebHostEnvironment _host;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public MailController(MailSettings mailSettings,
                              IWebHostEnvironment host,
                              IEmailSender emailSender,
                              RoleManager<IdentityRole> roleManager,
                              UserManager<ApplicationUser> userManager) : base(host)

        {
            _mailSettings = mailSettings;
            _host = host;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _userManager = userManager;
        }


        public ActionResult EmailSettings()
        {
            var mailSettings = _mailSettings;
            if (mailSettings == null)
            {
                return View("Notfound");
            }

            return View(mailSettings);
        }

        [HttpGet]
        public IActionResult SendEmail()
        {
            var emailVM = new EmailVM
            {
                MailSettings = _mailSettings
            };

            return View(emailVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendEmail(EmailVM emailVM)
        {
            //string filePath1 = Directory.GetCurrentDirectory() + "\\templates\\Confirm.html";
            //var filePath = _host.WebRootPath
            //                + Path.DirectorySeparatorChar + "templates"
            //                + Path.DirectorySeparatorChar + "EmailBootstrapTemplate.html";

            var filePath = _host.WebRootPath + "\\templates" + "\\Email.html";


            StreamReader htmlFile = new StreamReader(filePath);
            string content = htmlFile.ReadToEnd();
            htmlFile.Close();
            //تم استعماله مرتين: مرة ضمن الرسالة ومرة اخرى في عنوان الايميل ولكنه نفس العنوان// Subject
            content = content.Replace("{Subject}", emailVM.Subject); // يظهر داخل الرسالة
            content = content.Replace("{Content}", emailVM.Content);

            var message = new Message(new string[] { emailVM.To }, emailVM.Subject, content, emailVM.Attachments);

            try
            {
                await _emailSender.SendEmailAsync(message);
                TempData["SuccessMessage"] = "The email has been sent successfully";
            }
            catch
            {
                ViewBag.errorMessage = "Failed to send email";
                TempData["ErrorMessage"] = "Failed to send email";
            }

            emailVM.MailSettings = _mailSettings;
            return View(emailVM);

        }


        [HttpGet]
        public async Task<IActionResult> SendEmailToRole(string roleId)
        {
            if (roleId == null)
            {
                return View("NotFound");
            }

            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                ViewBag.Message = $"Cannot be found role with Id={roleId}";
                return View("NotFound");
            }


            var roleEmailsVM = new RoleEmailsVM
            {
                Id = role.Id,
                Name = role.Name,
                UsersCount = (await _userManager.GetUsersInRoleAsync(role.Name)).Count(),
                MailSettings = _mailSettings
            };

            return View(roleEmailsVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendEmailToRole(RoleEmailsVM roleEmailsVM)
        {
            string content = ReadHtmlTemplate("Email.html");

            content = content.Replace("{Subject}", roleEmailsVM.Subject); // يظهر داخل الرسالة
            content = content.Replace("{Content}", roleEmailsVM.Content);

            //var usersEmails1 = _userManager.Users.Select(u => u.Email).ToList();
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleEmailsVM.Name);
            
            if (usersInRole.Count == 0)
            {
                TempData["ErrorMessage"] = "No users in the role '" + roleEmailsVM.Name + "'";
                roleEmailsVM.MailSettings = _mailSettings;
                roleEmailsVM.UsersCount = 0;
                return View(roleEmailsVM);
            }


            //usersInRole = usersInRole.Where(u => u.EmailConfirmed).ToList();
            
            var usersEmails = usersInRole.Select(u => u.Email).ToList();


            var message = new Message(usersEmails.ToArray(), roleEmailsVM.Subject, content, roleEmailsVM.Attachments);

            try
            {
                await _emailSender.SendEmailAsync(message);
                TempData["SuccessMessage"] = "The email has been sent successfully";
            }
            catch
            {
                ViewBag.errorMessage = "Failed to send email";
                TempData["ErrorMessage"] = "Failed to send email";
            }

            roleEmailsVM.MailSettings = _mailSettings;
            roleEmailsVM.UsersCount = usersInRole.Count;

            return View(roleEmailsVM);
        }

    }
}
