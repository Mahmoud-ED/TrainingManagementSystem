using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;
using TrainingManagementSystem.ViewModels;
using System.Text.RegularExpressions;

namespace TrainingManagementSystem  .Controllers
{
    [ViewLayout("_LayoutDashboard")]

    [Authorize(Roles = "Admin,Prog,Employee")]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork<SiteState> _siteState;
        private readonly IUnitOfWork<SiteInfo> _siteInfo;
        private readonly IUnitOfWork<Contact> _contact;
 
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserSessionTracker _userSessionTracker;
        private readonly IWebHostEnvironment _host;
        private readonly IMapper _mapper;


        public AdminController(IUnitOfWork<SiteState> siteState,
                               IUnitOfWork<SiteInfo> siteInfo,
                               IUnitOfWork<Contact> contact,
                              
                               UserManager<ApplicationUser> userManager,
                                UserSessionTracker userSessionTracker,
                               IWebHostEnvironment host,
                               IMapper mapper)
        {
            _siteState = siteState;
            _siteInfo = siteInfo;
            _contact = contact;
         
            _userManager = userManager;
            _userSessionTracker = userSessionTracker;
            _host = host;
            _mapper = mapper;
        }



        [Route("Dashboard")]

        public async Task<IActionResult> Index()
        {
            ViewBag.usersCount = await _userManager.Users.CountAsync();
            ViewBag.ActiveUsers = _userSessionTracker.ActiveUsers;

            return View();
        }
      


        [Authorize(Roles = "Admin,Prog")]
        public async Task<IActionResult> SiteDetails()
        {
            return View(await GetSiteVM());
        }

        [Authorize(Roles = "Admin,Prog")]
        public async Task<IActionResult> SiteEdit()
        {
            return View(await GetSiteVM());
        }
        public async Task<SiteVM> GetSiteVM()
        {
            var siteInfo = await _siteInfo.Entity.GetAll().FirstOrDefaultAsync();
            var contact = await _contact.Entity.GetAll().FirstOrDefaultAsync();

            if (siteInfo == null || contact == null)
            {
                return new SiteVM();
            }

            var siteVM = new SiteVM
            {
                SiteInfo = siteInfo,
                Contact = contact
            };
            return siteVM;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SiteEdit(SiteVM siteVM, string? isImg1, string? isImg2)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string? logoFileName = UploadFile(siteVM.SiteInfo.Logo, siteVM.SiteInfo.LogoUrl, isImg1);
                    string? coverFileName = UploadFile(siteVM.SiteInfo.CoverImage, siteVM.SiteInfo.CoverImageUrl, isImg2);

                    var siteInfo = await _siteInfo.Entity.GetAll().FirstOrDefaultAsync();
                    if (siteInfo != null)
                    {
                        siteInfo.Name = siteVM.SiteInfo.Name;
                        siteInfo.Activity = siteVM.SiteInfo.Activity;
                        siteInfo.About = siteVM.SiteInfo.About;
                        siteInfo.LogoUrl = logoFileName;
                        siteInfo.CoverImageUrl = coverFileName;
                        siteInfo.Created = siteVM.SiteInfo.Created;
                        siteInfo.Modified = DateTime.Now;

                        _siteInfo.Entity.Update(siteInfo);
                    }
                    var contact = await _contact.Entity.GetAll().FirstOrDefaultAsync();
                    if (contact != null)
                    {
                        contact.Email = siteVM.Contact.Email;
                        contact.Phone = siteVM.Contact.Phone;
                        contact.Facebook = siteVM.Contact.Facebook;
                        contact.Twitter = siteVM.Contact.Twitter;
                        contact.Instagram = siteVM.Contact.Instagram;
                        contact.Created = siteVM.Contact.Created;
                        contact.Modified = DateTime.Now;

                        _contact.Entity.Update(contact);
                    }

                    await _siteInfo.SaveAsync();



                    TempData["SuccessMessage"] = "The website data has been successfully saved";
                }
                catch
                {
                    TempData["ErrorMessage"] = "Error Save";
                    throw;
                }

                return RedirectToAction("SiteDetails");

            }

            return RedirectToAction("SiteEdit");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SiteEdit1(SiteVM siteVM, string? isImg1, string? isImg2)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string? logoFileName = UploadFile(siteVM.SiteInfo.Logo, siteVM.SiteInfo.LogoUrl, isImg1);
                    string? coverFileName = UploadFile(siteVM.SiteInfo.CoverImage, siteVM.SiteInfo.CoverImageUrl, isImg2);


                    var contact = await _contact.Entity.GetAll().FirstOrDefaultAsync();
                    if (contact != null)
                    {
                        contact = _mapper.Map<Contact>(siteVM); // صيغة مختصرة

                        _contact.Entity.Update(contact);
                    }

                    var siteInfo = await _siteInfo.Entity.GetAll().FirstOrDefaultAsync();
                    if (siteInfo != null)
                    {
                        siteInfo = _mapper.Map<SiteVM, SiteInfo>(source: siteVM); // صيغة مطولة

                        siteInfo.LogoUrl = logoFileName;
                        siteInfo.CoverImageUrl = coverFileName;

                        _siteInfo.Entity.Update(siteInfo);
                    }

                    await _siteInfo.SaveAsync();
                    TempData["SuccessMessage"] = "The website data has been successfully saved";

                }
                catch
                {
                    TempData["ErrorMessage"] = "Error Save";
                    throw;
                }
                return RedirectToAction("SiteDetails");
            }

            return RedirectToAction("SiteEdit");
        }
        public string? UploadFile(IFormFile? img, string? imageUrl, string? isImg)
        {
            if (isImg == null) // في حال تم حذف الصورة فقط
            {
                DeleteOldFile(imageUrl);
                return null;
            }

            if (img != null)// في حال تم تحميل صورة جديدة
            {
                DeleteOldFile(imageUrl);

                string pictures = Path.Combine(_host.WebRootPath, "pictures");
                string NewPath = Path.Combine(pictures, img.FileName);
                if (!System.IO.File.Exists(NewPath))
                    img.CopyTo(new FileStream(NewPath, FileMode.CreateNew));

                return img.FileName;
            }
            return imageUrl; // في حال لم يتم تحميل صورة جديدة تبقى الصورة القديمة كما هي
        }
        public void DeleteOldFile(string? imageUrl)
        {
            if (imageUrl != null)
            {
                string picturesPath = Path.Combine(_host.WebRootPath, "pictures");
                string oldPath = Path.Combine(picturesPath, imageUrl);
                if (System.IO.File.Exists(oldPath))
                {
                    GC.Collect(); GC.WaitForPendingFinalizers();
                    System.IO.File.Delete(oldPath);
                }
            }
        }

        [AcceptVerbs("Get", "Post")]
        public async Task<JsonResult> EmailIsValid(Contact contact)
        {
            var regex = new Regex(@"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4})$");

            return Json(regex.IsMatch(contact.Email));

        }
        
        [Authorize(Roles = "Prog")]
        public async Task<IActionResult> SiteState(string? saveMessage)
        {
            ViewBag.Message = saveMessage;

            var siteState = await _siteState.Entity.GetAll().FirstOrDefaultAsync();
            if (siteState == null)
            {
                return View("NotFound");
            }
            return View(siteState);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SiteState(string save, [Bind("ClosingMessage")] SiteState siteStat)
        {
            try
            {
                string? message = null;

                var siteStateEdit = await _siteState.Entity.GetAll().FirstOrDefaultAsync();

                if (siteStateEdit == null)
                {
                    return View("NotFound");
                }

                if (save == "Activating Site")
                {
                    siteStateEdit.State = true;
                }
                else if (save == "Deactivate Site")
                {
                    siteStateEdit.State = false;
                }
                else if (save == "Update colsing message")
                {
                    siteStateEdit.ClosingMessage = siteStat.ClosingMessage;
                    message = "Save Success...";
                }

                siteStateEdit.Modified = DateTime.Now;
                _siteState.Entity.Update(siteStateEdit);
                await _siteState.SaveAsync();

                return RedirectToAction("SiteState", new { saveMessage = message });
            }
            catch
            {
                throw;
            }
        }


    }
}
