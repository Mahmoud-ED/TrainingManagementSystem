using Microsoft.AspNetCore.Mvc;
using System.Data;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace TrainingManagementSystem.Controllers
{

    [ViewLayout("_LayoutDashboard")]

    public class QualificationController : BaseController
    {

        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork<Qualification> _qualification;
        //private readonly IWebHostEnvironment _host;

        public QualificationController(
    ApplicationDbContext context,
                         IUnitOfWork<Qualification> qualification,
                              IWebHostEnvironment host) : base(host)
        {
            _context = context;
            _qualification = qualification;
        }

        public IActionResult Index()
        {

          var Qview=  _qualification.Entity.GetAll().ToList();
            return View(Qview);
        }



        // GET: Qualifications/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Qualifications/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Qualification qualification) // افترض أن BaseEntity يعتني بـ Id و CreatedAt تلقائيًا
        {
            if (ModelState.IsValid)
            {
                qualification.Modified= DateTime.Now;
                _qualification.Entity.Insert(qualification);
                await _qualification.SaveAsync();
                // يمكنك إضافة رسالة نجاح هنا باستخدام TempData
                TempData["SuccessMessage"] = "تم إضافة المؤهل بنجاح.";
                return RedirectToAction(nameof(Index));
            }
            return View(qualification);
        }


    }
}
