using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.ViewModels;
using TrainingManagementSystem.Models.Entities;

// افترض أن لديك DbContext باسم ApplicationDbContext
// using TrainingManagementSystem.Data; 

namespace TrainingManagementSystem.Controllers
{

     [ViewLayout("_LayoutDashboard")]
    [Authorize(Roles = "Admin,Prog")]
    public class PlanHeadersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // قم بحقن DbContext و IWebHostEnvironment
        public PlanHeadersController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: PlanHeaders (قائمة الخطط)
        public async Task<IActionResult> Index()
        {
            var plans = await _context.PlanHeaders.ToListAsync();
            return View(plans);
        }

        // GET: PlanHeaders/Details/5 (تفاصيل الخطة)
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();
            var planHeader = await _context.PlanHeaders.FirstOrDefaultAsync(m => m.Id == id);
            if (planHeader == null) return NotFound();
            return View(planHeader);
        }

        // GET: PlanHeaders/Create (عرض صفحة إنشاء خطة جديدة)
        public IActionResult Create()
        {
            return View();
        }

        // POST: PlanHeaders/Create (حفظ الخطة الجديدة)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Year,Quarter,Description,ApprovalFile,DateFrom,DateTo")] PlanHeader planHeader)
        {
            if (ModelState.IsValid)
            {
                // ---- منطق معالجة رفع الملف ----
                if (planHeader.ApprovalFile != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/plans");
                    // تأكد من وجود المجلد، إذا لم يكن موجوداً قم بإنشائه
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    // إنشاء اسم فريد للملف لمنع تكرار الأسماء
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + planHeader.ApprovalFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await planHeader.ApprovalFile.CopyToAsync(fileStream);
                    }
                    // حفظ المسار النسبي للملف في قاعدة البيانات
                    planHeader.ApprovalFileUrl = "/uploads/plans/" + uniqueFileName;
                }

                _context.Add(planHeader);
                await _context.SaveChangesAsync();
                // يمكنك إضافة رسالة نجاح هنا باستخدام TempData
                TempData["SuccessMessage"] = "Training Plan Created Successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(planHeader);
        }

        // GET: PlanHeaders/Edit/5 (عرض صفحة تعديل الخطة)
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            var planHeader = await _context.PlanHeaders.FindAsync(id);
            if (planHeader == null) return NotFound();
            return View(planHeader);
        }

        // POST: PlanHeaders/Edit/5 (حفظ التعديلات)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Year,Quarter,Description,ApprovalFile,DateFrom,DateTo,ApprovalFileUrl")] PlanHeader planHeader)
        {
            if (id != planHeader.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // ---- منطق تحديث الملف ----
                    if (planHeader.ApprovalFile != null)
                    {
                        // إذا كان هناك ملف قديم، قم بحذفه (اختياري)
                        if (!string.IsNullOrEmpty(planHeader.ApprovalFileUrl))
                        {
                            var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, planHeader.ApprovalFileUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        // رفع الملف الجديد
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/plans");
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + planHeader.ApprovalFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await planHeader.ApprovalFile.CopyToAsync(fileStream);
                        }
                        planHeader.ApprovalFileUrl = "/uploads/plans/" + uniqueFileName;
                    }

                    _context.Update(planHeader);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.PlanHeaders.Any(e => e.Id == planHeader.Id)) return NotFound();
                    else throw;
                }
                TempData["SuccessMessage"] = "Training Plan Updated Successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(planHeader);
        }

        // GET: PlanHeaders/Delete/5 (عرض صفحة تأكيد الحذف)
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            var planHeader = await _context.PlanHeaders.FirstOrDefaultAsync(m => m.Id == id);
            if (planHeader == null) return NotFound();
            return View(planHeader);
        }

        // POST: PlanHeaders/Delete/5 (تنفيذ الحذف)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var planHeader = await _context.PlanHeaders.FindAsync(id);
            if (planHeader != null)
            {
                // ---- حذف الملف المرتبط من السيرفر قبل حذف السجل من قاعدة البيانات ----
                if (!string.IsNullOrEmpty(planHeader.ApprovalFileUrl))
                {
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, planHeader.ApprovalFileUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.PlanHeaders.Remove(planHeader);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Training Plan Deleted Successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}