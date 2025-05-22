using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.Models.Interfaces;

namespace TrainingManagementSystem.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    public class DepartmentController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork<Department> _Department;

        public DepartmentController(
            ApplicationDbContext context,
            IUnitOfWork<Department> Department,
            IWebHostEnvironment host) : base(host)
        {
            _context = context;
            _Department = Department;
        }

        public IActionResult Index()
        {
            var Qview = _Department.Entity.GetAll().ToList();
            return View(Qview);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Department Department)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Departments.AnyAsync(q => q.Name == Department.Name))
                {
                    ModelState.AddModelError("Name", "الاسم  موجود مسبقًا.");
                    return View(Department);
                }
                Department.Id = Guid.NewGuid();
                Department.Modified = DateTime.Now; // Assuming Modified is similar to CreatedAt for new records
                _Department.Entity.Insert(Department);
                await _Department.SaveAsync();
                TempData["SuccessMessage"] = "تم الإضافة بنجاح.";
                return RedirectToAction(nameof(Index));
            }
            return View(Department);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "معرف غير صالح أو مفقود.";
                return RedirectToAction(nameof(Index));
            }

            var Department = await _context.Departments.FindAsync(id);

            if (Department == null)
            {
                TempData["ErrorMessage"] = "لم يتم العثور على المؤهل المطلوب تعديله.";
                return RedirectToAction(nameof(Index));
            }
            return View(Department);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,IsActive")] Department Department)
        {
            if (id != Department.Id)
            {
                TempData["ErrorMessage"] = "خطأ في تطابق معرف المؤهل.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Departments.AnyAsync(q => q.Name == Department.Name && q.Id != Department.Id))
                    {
                        ModelState.AddModelError("Name", "اسم المؤهل موجود مسبقًا لمؤهل آخر.");
                        return View(Department);
                    }
                    Department.Modified = DateTime.Now; // Update Modified timestamp
                    _context.Update(Department);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "تم تعديل المؤهل بنجاح!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(Department.Id))
                    {
                        TempData["ErrorMessage"] = "لم يتم العثور على المؤهل أثناء محاولة التحديث.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "حدث خطأ أثناء محاولة حفظ التغييرات. قد يكون السجل قد تم تعديله بواسطة مستخدم آخر.");
                        return View(Department);
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(Department);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "معرف المؤهل غير صالح أو مفقود.";
                return RedirectToAction(nameof(Index));
            }

            var Department = await _context.Departments
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Department == null)
            {
                TempData["ErrorMessage"] = "لم يتم العثور على المؤهل المطلوب حذفه.";
                return RedirectToAction(nameof(Index));
            }
            return View(Department);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var Department = await _context.Departments.FindAsync(id);

            if (Department == null)
            {
                TempData["ErrorMessage"] = "المؤهل غير موجود أو قد تم حذفه بالفعل.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                bool isUsedByTrainers = await _context.Trainees
                                              .AnyAsync(t => t.DepartmentId == id);

                if (isUsedByTrainers)
                {
                    TempData["ErrorMessage"] = $"لا يمكن حذف المؤهل '{Department.Name}' لأنه مستخدم من قبل متدرب واحد على الأقل. يرجى تحديث البيانات  أولاً.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Departments.Remove(Department);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"تم الحذف  '{Department.Name}' بنجاح.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = $"لا يمكن الحذف  '{Department.Name}'  يرجى إزالة الارتباطات أولاً.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "حدث خطأ غير متوقع أثناء محاولة حذف المؤهل.";
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentExists(Guid id)
        {
            return _context.Departments.Any(e => e.Id == id);
        }
    }
}
