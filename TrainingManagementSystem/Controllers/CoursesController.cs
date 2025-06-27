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
using TrainingManagementSystem.Models.Entities; // Your DbContext and Entities

namespace TrainingManagementSystem.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    [Authorize(Roles = "Admin,Prog")]
    [Display(Name = "تصنيفات البرامج التدريبية")] // ✅ Controller title

    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses
                                        .Include(c => c.CourseClassification)
                                        .Include(c => c.Level)
                                        .Include(c => c.CourseParent)
                                        .ToListAsync();
            return View(courses);
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.CourseClassification)
                .Include(c => c.Level)
                .Include(c => c.CourseParent)
                .Include(c => c.CourseDetails)
                    .ThenInclude(cd => cd.Status)
                .Include(c => c.CourseDetails)     // Eager load CourseDetails again to chain another .ThenInclude
                    .ThenInclude(cd => cd.Locations) // **** IF Location IS AN ENTITY ****
                .Include(c => c.CourseDetails)     // Eager load CourseDetails again
                    .ThenInclude(cd => cd.CourseTrainees)
                .Include(c => c.CourseTrainers)
                    .ThenInclude(ct => ct.Trainer)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }
        public async Task<IActionResult> SessionDetails(Guid id) // id here is CourseDetails.Id
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var courseDetailItem = await _context.CourseDetails
                                        .Include(cd => cd.Status)
                                        .Include(cd => cd.Course) // To show parent course info if needed
                                        .Include(cd => cd.CourseTrainees)
                                            .ThenInclude(cst => cst.Trainee) // To list trainees of this session
                                        .FirstOrDefaultAsync(cd => cd.Id == id);

            if (courseDetailItem == null)
            {
                return NotFound();
            }
            return View(courseDetailItem);
        }


        public async Task<IActionResult> Create()
        {
            var viewModel = new CreateCourseViewModel();
            await PopulateDropdownsAsync(viewModel); // مرر الـ ViewModel للدالة
            return View(viewModel);
        }
        private async Task PopulateDropdownsAsync(CreateCourseViewModel viewModel)
        {
            // ... (الكود السابق لملء باقي القوائم)
            viewModel.CourseClassifications = new SelectList(await _context.CourseClassifications.OrderBy(cc => cc.Name).ToListAsync(), "Id", "Name");
            viewModel.Levels = new SelectList(await _context.Levels.OrderBy(l => l.Name).ToListAsync(), "Id", "Name");
            viewModel.ParentCourses = new SelectList(await _context.CourseParent.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");

            // --- الإضافة الجديدة: ملء قائمة المدربين ---
            // لاحظ أن القيمة "Value" هي Guid هنا
            viewModel.AllTrainers = new SelectList(await _context.Trainers.OrderBy(t => t.ArName).ToListAsync(), "Id", "ArName");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCourseViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // استخدام Transaction لضمان حفظ الدورة ومدربيها معاً أو لا شيء
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // الخطوة 1: إنشاء وحفظ الدورة أولاً للحصول على ID
                        var course = new Course
                        {
                            Days= viewModel.Days,
                            DurationHours = viewModel.DurationHours,
                            Name = viewModel.Name,
                            Code = viewModel.Code,
                            Description = viewModel.Description,
                            CourseClassificationId = viewModel.CourseClassificationId,
                            LevelId = viewModel.LevelId,
                            CourseParentId = viewModel.CourseParentId,
                            Created = DateTime.Now // أو أي قيمة افتراضية أخرى
                        };

                        _context.Courses.Add(course);
                        await _context.SaveChangesAsync(); // الحفظ هنا ضروري لتوليد course.Id

                        // الخطوة 2: إضافة المدربين المختارين إلى جدول الربط
                        if (viewModel.SelectedTrainerIds != null && viewModel.SelectedTrainerIds.Any())
                        {
                            foreach (var trainerId in viewModel.SelectedTrainerIds)
                            {
                                var courseTrainer = new CourseTrainer
                                {
                                    CourseId = course.Id, // <- استخدام الـ ID الجديد للدورة
                                    TrainerId = trainerId
                                };
                                _context.CourseTrainers.Add(courseTrainer);
                            }
                            // حفظ سجلات جدول الربط
                            await _context.SaveChangesAsync();
                        }

                        // إذا تم كل شيء بنجاح، قم بتثبيت التغييرات
                        await transaction.CommitAsync();

                        // يمكنك إضافة رسالة نجاح هنا باستخدام TempData
                        TempData["SuccessMessage"] = "تم إنشاء الدورة بنجاح!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        // في حالة حدوث أي خطأ، تراجع عن كل التغييرات
                        await transaction.RollbackAsync();

                        // سجل الخطأ (مهم جداً لتصحيح الأخطاء لاحقاً)
                        // _logger.LogError(ex, "An error occurred while creating a course.");

                        // أضف رسالة خطأ للمستخدم
                        ModelState.AddModelError("", "حدث خطأ غير متوقع أثناء حفظ الدورة. يرجى المحاولة مرة أخرى.");
                    }
                }
            }

            // إذا كان النموذج غير صالح، أعد ملء القوائم وأرجع المستخدم لنفس الصفحة
            await PopulateDropdownsAsync(viewModel);
            return View(viewModel);
        }


        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.CourseClassification)
                .Include(c => c.Level)
                .Include(c => c.CourseParent)
                .Include(c => c.CourseDetails)
                    .ThenInclude(cd => cd.Status)
                .Include(c => c.CourseDetails)     // Eager load CourseDetails again to chain another .ThenInclude
                    .ThenInclude(cd => cd.  Locations   ) // **** IF Location IS AN ENTITY ****
                .Include(c => c.CourseDetails)     // Eager load CourseDetails again
                    .ThenInclude(cd => cd.CourseTrainees)
                .Include(c => c.CourseTrainers)
                    .ThenInclude(ct => ct.Trainer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null)
            {
                return NotFound();
            }
            await PopulateDropdownsAsyncEdit(course);
            return View(course);
        }


        private async Task PopulateDropdownsAsyncEdit(Course course = null)
        {
            ViewBag.CourseClassificationId = new SelectList(await _context.CourseClassifications.OrderBy(cc => cc.Name).ToListAsync(), "Id", "Name", course?.CourseClassificationId);
            ViewBag.LevelId = new SelectList(await _context.Levels.OrderBy(l => l.Name).ToListAsync(), "Id", "Name", course?.LevelId);
            ViewBag.CourseParentId = new SelectList(await _context.CourseParent.OrderBy(l => l.Name).ToListAsync(), "Id", "Name", course?.CourseParentId);


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,CourseClassificationId,LevelId,Name,Code,Description,CourseParentId,CreatedAt")] Course course)
        {
            if (id != course.Id)
            {
                return NotFound();
            }

            //if (ModelState.IsValid)
            //{
                try
                {
                    course.Modified = DateTime.UtcNow;
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            //}
            //await PopulateDropdownsAsync(course);
            //return View(course);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.CourseClassification)
                .Include(c => c.Level)
                .Include(c => c.CourseParent)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(Guid id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }

  
    }
}