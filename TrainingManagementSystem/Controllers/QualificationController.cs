using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace TrainingManagementSystem.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    [Display(Name = "المؤهلات")] // ✅ Controller title

    public class QualificationController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork<Qualification> _qualification;

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
            var Qview = _qualification.Entity.GetAll().ToList();
            return View(Qview);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,IsActive")] Qualification qualification)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Qualifications.AnyAsync(q => q.Name == qualification.Name))
                {
                    ModelState.AddModelError("Name", "اسم المؤهل موجود مسبقًا.");
                    return View(qualification);
                }
                qualification.Created = DateTime.Now; // Assuming Modified is similar to CreatedAt for new records
                _qualification.Entity.Insert(qualification);
                await _qualification.SaveAsync();
                TempData["SuccessMessage"] = "تم إضافة المؤهل بنجاح.";
                return RedirectToAction(nameof(Index));
            }
            return View(qualification);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "معرف المؤهل غير صالح أو مفقود.";
                return RedirectToAction(nameof(Index));
            }

            var qualification = await _context.Qualifications.FindAsync(id);

            if (qualification == null)
            {
                TempData["ErrorMessage"] = "لم يتم العثور على المؤهل المطلوب تعديله.";
                return RedirectToAction(nameof(Index));
            }
            return View(qualification);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,IsActive")] Qualification qualification)
        {
            if (id != qualification.Id)
            {
                TempData["ErrorMessage"] = "خطأ في تطابق معرف المؤهل.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Qualifications.AnyAsync(q => q.Name == qualification.Name && q.Id != qualification.Id))
                    {
                        ModelState.AddModelError("Name", "اسم المؤهل موجود مسبقًا لمؤهل آخر.");
                        return View(qualification);
                    }
                    qualification.Modified = DateTime.Now; // Update Modified timestampa
                    _context.Update(qualification);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "تم تعديل المؤهل بنجاح!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QualificationExists(qualification.Id))
                    {
                        TempData["ErrorMessage"] = "لم يتم العثور على المؤهل أثناء محاولة التحديث.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "حدث خطأ أثناء محاولة حفظ التغييرات. قد يكون السجل قد تم تعديله بواسطة مستخدم آخر.");
                        return View(qualification);
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(qualification);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "معرف المؤهل غير صالح أو مفقود.";
                return RedirectToAction(nameof(Index));
            }

            var qualification = await _context.Qualifications
                .FirstOrDefaultAsync(m => m.Id == id);

            if (qualification == null)
            {
                TempData["ErrorMessage"] = "لم يتم العثور على المؤهل المطلوب حذفه.";
                return RedirectToAction(nameof(Index));
            }
            return View(qualification);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var qualification = await _context.Qualifications.FindAsync(id);

            if (qualification == null)
            {
                TempData["ErrorMessage"] = "المؤهل غير موجود أو قد تم حذفه بالفعل.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                bool isUsedByTrainers = await _context.Trainers
                                              .AnyAsync(t => t.QualificationId == id);

                if (isUsedByTrainers)
                {
                    TempData["ErrorMessage"] = $"لا يمكن حذف المؤهل '{qualification.Name}' لأنه مستخدم من قبل مدرب واحد على الأقل. يرجى تحديث بيانات المدربين أولاً.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Qualifications.Remove(qualification);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"تم حذف المؤهل '{qualification.Name}' بنجاح.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = $"لا يمكن حذف المؤهل '{qualification.Name}' لأنه مرتبط بسجلات أخرى (مثل المدربين). يرجى إزالة الارتباطات أولاً.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "حدث خطأ غير متوقع أثناء محاولة حذف المؤهل.";
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

        private bool QualificationExists(Guid id)
        {
            return _context.Qualifications.Any(e => e.Id == id);
        }
    }
}