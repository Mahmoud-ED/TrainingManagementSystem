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
            //if (ModelState.IsValid)
            //{
                // استخدام Transaction لضمان حفظ الدورة ومدربيها معاً أو لا شيء
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {

                   
                        var course = new Course
                        {
                            Days= viewModel.Days,
                            DurationHours = viewModel.DurationHours,
                            Name = viewModel.Name,
                            Code = viewModel.Code,
                            Description = viewModel.Description?? " ",
                            CourseClassificationId = viewModel.CourseClassificationId,
                            LevelId = null,
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


                        // أضف رسالة خطأ للمستخدم
                        ModelState.AddModelError("", "حدث خطأ غير متوقع أثناء حفظ الدورة. يرجى المحاولة مرة أخرى.");
                    }
                }
            //}

            //// إذا كان النموذج غير صالح، أعد ملء القوائم وأرجع المستخدم لنفس الصفحة
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
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,CourseClassificationId,Name,Code,Description,CreatedAt")] Course course)
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

        // GET: Courses/ManagePrerequisites/5
        [HttpGet]
        public async Task<IActionResult> ManagePrerequisites(Guid id)
        {
            // 1. ابحث عن الكورس الأساسي
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            // 2. احصل على قائمة بالمتطلبات الحالية لهذا الكورس
            var currentPrerequisites = await _context.CoursePrerequisites
                .Where(p => p.CourseId == id)
                .Select(p => p.PrerequisiteCourse)
                .ToListAsync();

            // 3. احصل على قائمة بكل الكورسات الأخرى في النظام (باستثناء الكورس الحالي ومتطلباته الحالية)
            var currentPrerequisiteIds = currentPrerequisites.Select(c => c.Id).ToList();
            var availableCourses = await _context.Courses
                .Where(c => c.Id != id && !currentPrerequisiteIds.Contains(c.Id))
                .OrderBy(c => c.Name)
                .ToListAsync();

            // 4. قم بإنشاء ViewModel لتمرير كل هذه البيانات إلى الـ View
            var viewModel = new ManagePrerequisitesViewModel
            {
                Course = course,
                CurrentPrerequisites = currentPrerequisites,
                AvailableCourses = availableCourses
            };

            return View(viewModel);
        }

        // POST: Courses/AddPrerequisite
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPrerequisite(Guid courseId, Guid prerequisiteCourseId)
        {
            // تحقق من أن الكورسين موجودان
            var courseExists = await _context.Courses.AnyAsync(c => c.Id == courseId);
            var prerequisiteExists = await _context.Courses.AnyAsync(c => c.Id == prerequisiteCourseId);

            if (!courseExists || !prerequisiteExists || courseId == prerequisiteCourseId)
            {
                // يمكنك إرجاع رسالة خطأ أكثر تفصيلاً هنا إذا أردت
                return BadRequest("بيانات غير صالحة.");
            }

            // تحقق مما إذا كان هذا المتطلب موجودًا بالفعل لتجنب التكرار
            var alreadyExists = await _context.CoursePrerequisites
                .AnyAsync(p => p.CourseId == courseId && p.PrerequisiteCourseId == prerequisiteCourseId);

            if (alreadyExists)
            {
                // يمكنك إضافة رسالة للمستخدم عبر TempData
                TempData["ErrorMessage"] = "هذا المتطلب موجود بالفعل.";
                return RedirectToAction("ManagePrerequisites", new { id = courseId });
            }

            var newPrerequisite = new CoursePrerequisite
            {
                CourseId = courseId,
                PrerequisiteCourseId = prerequisiteCourseId
            };

            _context.CoursePrerequisites.Add(newPrerequisite);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تمت إضافة المتطلب بنجاح.";
            return RedirectToAction("ManagePrerequisites", new { id = courseId });
        }

        // POST: Courses/RemovePrerequisite
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePrerequisite(Guid courseId, Guid prerequisiteCourseId)
        {
            var prerequisiteToRemove = await _context.CoursePrerequisites
                .FirstOrDefaultAsync(p => p.CourseId == courseId && p.PrerequisiteCourseId == prerequisiteCourseId);

            if (prerequisiteToRemove == null)
            {
                return NotFound();
            }

            _context.CoursePrerequisites.Remove(prerequisiteToRemove);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تمت إزالة المتطلب بنجاح.";
            return RedirectToAction("ManagePrerequisites", new { id = courseId });
        }


        [HttpGet]
        public async Task<IActionResult> StudyPlan()
        {
            // 1. جلب كل الدورات التي لها مستوى وتصنيف
            var allCourses = await _context.Courses
                .Where(c => c.LevelId.HasValue)
                .AsNoTracking()
                .ToListAsync();

            // 2. إنشاء خرائط (Dictionaries) لتحويل GUIDs إلى أرقام صفوف وأعمدة
            var levelToRowMap = allCourses
                .Select(c => c.LevelId.Value)
                .Distinct()
                .Select((levelId, index) => new { levelId, row = index + 1 })
                .ToDictionary(item => item.levelId, item => item.row);

            var classificationToColMap = allCourses
                .Select(c => c.CourseClassificationId)
                .Distinct()
                .Select((classId, index) => new { classId, col = index + 1 })
                .ToDictionary(item => item.classId, item => item.col);

            // 3. تحويل الدورات إلى CourseNodeViewModel مع تحديد الصف والعمود
            var courseNodes = allCourses.Select(c => new CourseNodeViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                DurationHours = c.DurationHours,
                Row = levelToRowMap.ContainsKey(c.LevelId.Value) ? levelToRowMap[c.LevelId.Value] : 0,
                Column = classificationToColMap.ContainsKey(c.CourseClassificationId) ? classificationToColMap[c.CourseClassificationId] : 0
            }).ToList();

            // 4. جلب علاقات المتطلبات
            var prerequisiteLinks = await _context.CoursePrerequisites
                .AsNoTracking()
                .Select(p => new PrerequisiteLinkViewModel
                {
                    // السهم يخرج من المتطلب المسبق (Source) إلى الدورة التالية (Target)
                    SourceId = "course-" + p.PrerequisiteCourseId,
                    TargetId = "course-" + p.CourseId
                }).ToListAsync();

            // 5. تجميع كل شيء في الـ ViewModel الرئيسي
            var finalViewModel = new CoursePlanViewModel
            {
                Courses = courseNodes,
                Prerequisites = prerequisiteLinks,
                MaxRow = levelToRowMap.Values.DefaultIfEmpty(0).Max(),
                MaxColumn = classificationToColMap.Values.DefaultIfEmpty(0).Max()
            };

            return View("StudyPlan", finalViewModel); // سننشئ View باسم StudyPlan.cshtml
        }


        // هذه الدالة المساعدة تقوم ببناء الهيكل الشجري
    }
}