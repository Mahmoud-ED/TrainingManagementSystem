using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;

namespace TrainingManagementSystem.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    public class SpecializationsController : BaseController
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
        private readonly IUnitOfWork<Specialization> _specialization;

        public SpecializationsController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 IWebHostEnvironment host,
                                 IEmailSender emailSender,
                                 UserSessionTracker userSessionTracker,
                                 RoleManager<IdentityRole> roleManager,
                                 IUnitOfWork<Employee> employee,
                                 IUnitOfWork<UserProfile> userProfile,
                                 IUnitOfWork<Specialization> specialization,
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
            _specialization = specialization;
        }

        // GET: Specializations
        public async Task<IActionResult> Index()
        {
            // جلب جميع التخصصات من قاعدة البيانات
            var specializations = await _specialization.Entity.GetAll().ToListAsync();
            return View(specializations); // إرسال قائمة التخصصات إلى الواجهة
        }


        // GET: SpecializationsController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: SpecializationsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: SpecializationsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: SpecializationsController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: SpecializationsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: SpecializationsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: SpecializationsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
