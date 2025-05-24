using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // ✅ Add this for DisplayAttribute
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;

namespace TrainingManagementSystem.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    [Authorize(Roles = "Admin,Prog")]
    [Display(Name = "تصنيفات البرامج التدريبية")] // ✅ Controller title
    public class CourseClassificationController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork<CourseClassification> _courseClassification;

        public CourseClassificationController(ApplicationDbContext context,
                                              IUnitOfWork<CourseClassification> courseClassification,
                                              IWebHostEnvironment host) : base(host)
        {
            _context = context;
            _courseClassification = courseClassification;
        }

        [Display(Name = "قائمة التصنيفات")] // ✅ Action title
        public ActionResult Index()
        {
            var CourseClassification = _courseClassification.Entity.GetAll().ToList();
            return View(CourseClassification);
        }

        [Display(Name = "تفاصيل التصنيف")] // ✅ Action title
        public async Task<IActionResult> Details(Guid? id)
        {
            var courseClassification = await _courseClassification.Entity.GetByIdAsync(id);
            return View(courseClassification);
        }

        [Display(Name = "إضافة تصنيف جديد")] // ✅ Action title
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Display(Name = "حفظ التصنيف الجديد")] // ✅ Action title
        public async Task<IActionResult> Create([Bind("Name")] CourseClassification courseClassification)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var exists = await _courseClassification.Entity
                                      .GetWhere(a => a.Name == courseClassification.Name)
                                      .FirstOrDefaultAsync();

                    if (exists != null)
                    {
                        ViewBag.Message = $"CourseClassificarion '{exists.Name}' تمت إضافته مسبقا";
                        return View();
                    }
                    courseClassification.Name = courseClassification.Name.Trim();
                    courseClassification.Created = DateTime.Now;
                    _courseClassification.Entity.Insert(courseClassification);
                    await _courseClassification.SaveAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch
                {
                    throw;
                }
            }
            return View(courseClassification);
        }

        [AcceptVerbs("Get", "Post")]
        public async Task<JsonResult> NameExists(string name)
        {
            var exists = await _courseClassification.Entity.GetAll()
                                  .FirstOrDefaultAsync(n => n.Name == name.Trim());

            if (exists == null)
            {
                return Json(true);
            }
            else
            {
                return Json($"التصنيف '{exists.Name}' مستخدم مسبقاَ");
            }
        }

        [Display(Name = "تعديل التصنيف")] // ✅ Action title
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return View("NotFound");
            }

            var courseClassification = await _courseClassification.Entity.GetByIdAsync(id);

            if (courseClassification == null)
            {
                ViewBag.Message = "التصنيف ليس موجود";
                return View("NotFound");
            }
            return View(courseClassification);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Display(Name = "تحديث التصنيف")] // ✅ Action title
        public async Task<IActionResult> Edit(Guid id, [Bind("Name,Id,Created,Modified")] CourseClassification courseClassification)
        {
            if (id != courseClassification.Id)
            {
                return View("NotFound");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var courseClassificationNameExists = await _courseClassification.Entity
                                            .GetWhere(a => a.Name == courseClassification.Name.Trim() & a.Id != courseClassification.Id)
                                            .FirstOrDefaultAsync();

                    if (courseClassificationNameExists != null)
                    {
                        ViewBag.Message = $"CourseClassification '{courseClassification.Name}' تمت إضافتها بالفعل";
                        return View(courseClassification);
                    }

                    _courseClassification.Entity.Update(courseClassification);
                    await _courseClassification.SaveAsync();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!CourseClassificationExists(courseClassification.Id))
                    {
                        return View("NotFound");
                    }
                    else
                    {
                        ViewBag.ErrorTitle = "The basic data not found in the database ";
                        ViewBag.ErrorMessage = "Missing data row- " + ex;
                        return View("Error");
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(courseClassification);
        }

        private bool CourseClassificationExists(Guid id)
        {
            return (_courseClassification.Entity.GetAll()?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        [Display(Name = "عرض حذف التصنيف")] // ✅ Action title
        public ActionResult Delete(int id)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Display(Name = "تأكيد حذف التصنيف")] // ✅ Action title
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return View("NotFound");
            }

            var section = await _courseClassification.Entity.GetByIdAsync(id);
            if (section == null)
            {
                return View("NotFound");
            }

            return View(section);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Display(Name = "حذف نهائي للتصنيف")] // ✅ Action title
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var courseClassification = await _courseClassification.Entity.GetByIdAsync(id);
            if (courseClassification == null)
            {
                return View("NotFound");
            }

            try
            {
                _courseClassification.Entity.Delete(courseClassification);
                await _courseClassification.SaveAsync();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                throw;
            }
        }
    }
}
