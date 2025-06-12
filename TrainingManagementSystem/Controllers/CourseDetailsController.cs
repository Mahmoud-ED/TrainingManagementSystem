using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.ViewModels;

namespace TrainingManagementSystem.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    [Authorize(Roles = "Admin,Prog")]
    public class CourseDetailsController : Controller
    {
        private readonly ApplicationDbContext _context; // استبدل ApplicationDbContext باسم الـ DbContext الخاص بك

        public CourseDetailsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Courses
        public async Task<IActionResult> Index()
        {
            // 1. جلب البيانات من قاعدة البيانات كما فعلت سابقاً
            var courseDetailsList = await _context.CourseDetails
                                        .Include(cd => cd.Course)
                                        .Include(cd => cd.Location)
                                        .Include(cd => cd.CourseType)
                                        .Include(cd => cd.Status)
                                        .Include(cd => cd.CourseTrainees)
                                        .ToListAsync();

            // 2. حساب الإحصائيات المطلوبة
            var upcomingCourses = courseDetailsList
                                    .Where(cd => cd.Status?.Name == "Open" && cd.StartDate >= DateTime.Today)
                                    .OrderBy(cd => cd.StartDate)
                                    .ToList();

            var viewModel = new CourseDetailsIndexViewModel
            {
                // أ. تعبئة القائمة الرئيسية
                CourseDetailsList = courseDetailsList,

                // ب. تعبئة بيانات التقرير
                ActiveCoursesCount = courseDetailsList.Count(cd => cd.Status?.Name == "Open"),
                CompletedCoursesCount = courseDetailsList.Count(cd => cd.Status?.Name == "Close"),
                PostponedCoursesCount = courseDetailsList.Count(cd => cd.Status?.Name == "Next"),
                CancelledCoursesCount = courseDetailsList.Count(cd => cd.Status?.Name == "Cansel"),
                UpcomingCoursesCount = upcomingCourses.Count, // نستخدم القائمة المحسوبة مسبقاً
                NextUpcomingCourse = upcomingCourses.FirstOrDefault(), // أقرب دورة قادمة
                TotalRegisteredTrainees = courseDetailsList.Sum(cd => cd.CourseTrainees?.Count ?? 0)
            };

            // 3. إرسال الـ ViewModel إلى الـ View
            return View(viewModel);
        }


        public async Task<IActionResult> AnnualPlan()
        {
            // 1. تحديد السنة الحالية
            int year = DateTime.Today.Year;

            // 2. جلب جميع تفاصيل الدورات للسنة المحددة
            var allCourseDetails = await _context.CourseDetails
                                        .Include(cd => cd.Course)
                                        .Include(cd => cd.Location)
                                        .Include(cd => cd.Status)
                                        .Where(cd => cd.StartDate.Year == year) // فلترة حسب السنة الحالية
                                        .OrderBy(cd => cd.StartDate) // ترتيب حسب تاريخ البدء
                                        .ToListAsync();

            // 3. إنشاء ViewModel وتعبئته
            var viewModel = new AnnualPlanViewModel
            {
                Year = year,
                TotalCoursesInYear = allCourseDetails.Count
            };

            // 4. توزيع الدورات على الأرباع
            foreach (var course in allCourseDetails)
            {
                // صيغة بسيطة لتحديد الربع من الشهر: (Month-1)/3 + 1
                int quarter = (course.StartDate.Month - 1) / 3 + 1;

                if (viewModel.CoursesByQuarter.ContainsKey(quarter))
                {
                    viewModel.CoursesByQuarter[quarter].Add(course);
                }
            }

            // 5. إرسال الـ ViewModel إلى الـ View الجديد
            return View("AnnualPlan", viewModel); // تأكد من أن اسم الـ View هو AnnualPlan
        }

        // GET: CoursesDetalis/Create
        public async Task<IActionResult> Create( Guid? id)
        {

            var course1 = await _context.Courses
              .Include(c => c.CourseClassification)
              .Include(c => c.Level)
              .Include(c => c.CourseParent)
              .Include(c => c.CourseDetails)
                  .ThenInclude(cd => cd.Status)
              .Include(c => c.CourseDetails)     // Eager load CourseDetails again to chain another .ThenInclude
                  .ThenInclude(cd => cd.Location) // **** IF Location IS AN ENTITY ****
              .Include(c => c.CourseDetails)     // Eager load CourseDetails again
                  .ThenInclude(cd => cd.CourseTrainees)
              .Include(c => c.CourseTrainers)
                  .ThenInclude(ct => ct.Trainer)
              .FirstOrDefaultAsync(m => m.Id == id);


            var courseList = await _context.Courses
            .Include(c => c.CourseClassification)
            .Include(c => c.Level)
            .Include(c => c.CourseParent)
            .Include(c => c.CourseDetails)
                .ThenInclude(cd => cd.Status)
            .Include(c => c.CourseDetails)     // Eager load CourseDetails again to chain another .ThenInclude
                .ThenInclude(cd => cd.Location) // **** IF Location IS AN ENTITY ****
            .Include(c => c.CourseDetails)     // Eager load CourseDetails again
                .ThenInclude(cd => cd.CourseTrainees)
            .Include(c => c.CourseTrainers)
                .ThenInclude(ct => ct.Trainer).ToListAsync();
         
            
            var viewModel = new CourseFormViewModel
            {
               Course=course1,
                CourseList = courseList,
                CourseClassifications = (await _context.CourseClassifications
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .ToListAsync()),

                Levels = (await _context.Levels
                    .OrderBy(l => l.Name)
                    .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name })
                    .ToListAsync()),

                CourseParents = (await _context.CourseParent
                    .OrderBy(cp => cp.Name)
                    .Select(cp => new SelectListItem { Value = cp.Id.ToString(), Text = cp.Name })
                    .ToListAsync()),

            };

            // إضافة خيار فارغ للقوائم المنسدلة
            viewModel.CourseClassifications.Insert(0, new SelectListItem { Value = "", Text = "-- اختر تصنيف الدورة --" });
            viewModel.Levels.Insert(0, new SelectListItem { Value = "", Text = "-- اختر المستوى --" });
            viewModel.CourseParents.Insert(0, new SelectListItem { Value = "", Text = "-- اختر الدورة المرجعية --" });
           
            return View(viewModel);
        }


        // POST: Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CourseFormViewModel viewModel)
        {
            // التحقق من صحة المدخلات لـ CourseDetailsEntries يدوياً إذا لم يكن [Required] كافياً
            // أو الاعتماد على DataAnnotations في CourseDetailFormEntryViewModel
            if (viewModel.CourseDetailsEntries == null || !viewModel.CourseDetailsEntries.Any())
            {
                // يمكنك إضافة خطأ إذا كان يجب إضافة تفصيل واحد على الأقل
                ModelState.AddModelError("CourseDetailsEntries", "يجب إضافة تفصيل واحد على الأقل للدورة.");
            }
     
            // الجديد: إضافة تفاصيل الدورة (CourseDetails)
            if (viewModel.CourseDetailsEntries != null)
            {
                foreach (var entryVm in viewModel.CourseDetailsEntries)
                {
                    // تحقق إضافي لكل عنصر إذا لزم الأمر
                    if (entryVm.DurationHours > 0) // مثال على تحقق بسيط
                    {
                        var courseDetail = new CourseDetails
                        {
                            Created = DateTime.Now, // تعيين تاريخ الإنشاء    ss
                            DurationHours = entryVm.DurationHours,
                            StartDate = entryVm.StartDate,
                            EndDate = entryVm.EndDate,
                            LocationId = entryVm.LocationId,
                            CourseTypeId = entryVm.CourseTypeId,
                            StatusId = entryVm.StatusId,
                            CourseId= viewModel.Id, // ربط الدورة بالتفاصيل
                        };
                        _context.CourseDetails.Add(courseDetail);
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "تم إنشاء الدورة وتفاصيلها بنجاح!";
            return RedirectToAction(nameof(Index));

        }


        [HttpGet]
        public async Task<IActionResult> GetCourseDetailEntryPartial(int index)
        {
            var parentViewModelForDropdowns = new CourseFormViewModel
            {

                Locations = (await _context.Locations
              .OrderBy(l => l.Name)
              .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name })
              .ToListAsync()),

                CourseTypes = (await _context.CourseTypes
              .OrderBy(ct => ct.Name)
              .Select(ct => new SelectListItem { Value = ct.Id.ToString(), Text = ct.Name })
              .ToListAsync()),

                Statuses = (await _context.Statuses
              .OrderBy(s => s.Name)
              .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
              .ToListAsync())
            };

            // إضافة عنصر اختياري فارغ لكل Dropdown
            parentViewModelForDropdowns.Locations.Insert(0, new SelectListItem { Value = "", Text = "-- اختر الموقع --" });
            parentViewModelForDropdowns.CourseTypes.Insert(0, new SelectListItem { Value = "", Text = "-- اختر نوع الدورة --" });
            parentViewModelForDropdowns.Statuses.Insert(0, new SelectListItem { Value = "", Text = "-- اختر الحالة --" });


            ViewData["index"] = index;
            ViewData["ParentViewModel"] = parentViewModelForDropdowns;
            ViewData["ParentListSize"] = new List<CourseDetailFormEntryViewModel>();

            // نرجع الـ Partial View مع Model جديد فارغ (أو مهيأ بقيم افتراضية إذا أردت)
            return PartialView("_CourseDetailFormEntry", new CourseDetailFormEntryViewModel { StartDate = DateTime.Today });
        }

        // ... (بقية الـ Actions في CoursesController)



        private async Task PopulateFormViewModelDropdowns(CourseFormViewModel viewModel)
        {
            viewModel.CourseClassifications = (await _context.CourseClassifications
      .OrderBy(c => c.Name)
      .Select(c => new SelectListItem
      {
          Value = c.Id.ToString(),
          Text = c.Name,
          Selected = (c.Id == viewModel.CourseClassificationId)
      })
      .ToListAsync());

            viewModel.CourseClassifications.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- اختر تصنيف الدورة --",
                Selected = (viewModel.CourseClassificationId == Guid.Empty)
            });
            viewModel.Levels = (await _context.Levels
     .OrderBy(l => l.Name)
     .Select(l => new SelectListItem
     {
         Value = l.Id.ToString(),
         Text = l.Name,
         Selected = (l.Id == viewModel.LevelId)
     })
     .ToListAsync());

            viewModel.Levels.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- اختر المستوى --",
                Selected = (viewModel.LevelId == Guid.Empty)
            });
            viewModel.CourseParents = (await _context.CourseParent
      .OrderBy(cp => cp.Name)
      .Select(cp => new SelectListItem
      {
          Value = cp.Id.ToString(),
          Text = cp.Name,
          Selected = (cp.Id == viewModel.CourseParentId)
      })
      .ToListAsync());

            viewModel.CourseParents.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- اختر الدورة المرجعية --",
                Selected = (!viewModel.CourseParentId.HasValue || viewModel.CourseParentId == Guid.Empty)
            });

            var allTrainers = await _context.Trainers.OrderBy(t => t.ArName).ToListAsync();
            viewModel.AvailableTrainers = allTrainers.Select(t => new TrainerCheckboxViewModel
            {
                Id = t.Id,
                Name = t.ArName,
                IsSelected = viewModel.SelectedTrainerIds != null && viewModel.SelectedTrainerIds.Contains(t.Id)
            }).ToList();

            viewModel.Locations = (await _context.Locations
      .OrderBy(l => l.Name)
      .Select(l => new SelectListItem
      {
          Value = l.Id.ToString(),
          Text = l.Name
      })
      .ToListAsync());

            viewModel.Locations.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- اختر الموقع --"
            });
            viewModel.CourseTypes = (await _context.CourseTypes
      .OrderBy(ct => ct.Name)
      .Select(ct => new SelectListItem
      {
          Value = ct.Id.ToString(),
          Text = ct.Name
      })
      .ToListAsync());

            viewModel.CourseTypes.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- اختر نوع الدورة --"
            });
            viewModel.Statuses = (await _context.Statuses
      .OrderBy(s => s.Name)
      .Select(s => new SelectListItem
      {
          Value = s.Id.ToString(),
          Text = s.Name
      })
      .ToListAsync());

            viewModel.Statuses.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- اختر الحالة --"
            });

            // إذا كنت تريد إعادة ملء قيم CourseDetailsEntries عند الفشل، فهذا أكثر تعقيداً
            // لأنك تحتاج إلى الحفاظ على القيم التي أدخلها المستخدم.
            // الـ Model Binding سيفعل ذلك تلقائياً إذا كانت أسماء الحقول صحيحة.
        }
        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // ابحث عن تفاصيل الدورة المراد تعديلها
            var courseDetails = await _context.CourseDetails.FindAsync(id);
            if (courseDetails == null)
            {
                return NotFound();
            }

            // دالة مساعدة لتعبئة القوائم المنسدلة
            await PopulateDropdownsAsync(courseDetails);

            return View(courseDetails);
        }

        // POST: CourseDetails/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,CourseId,DurationHours,StartDate,EndDate,LocationId,CourseTypeId,StatusId")] CourseDetails courseDetails)
        {
            if (id != courseDetails.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(courseDetails);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseDetailsExists(courseDetails.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                // بعد الحفظ بنجاح، قم بإعادة التوجيه إلى صفحة القائمة الرئيسية
                return RedirectToAction(nameof(Index));
            }

            // في حال وجود خطأ في الإدخال، أعد تعبئة القوائم المنسدلة وأعد عرض الفورم
            await PopulateDropdownsAsync(courseDetails);
            return View(courseDetails);
        }

        // دالة مساعدة لتجنب تكرار الكود
        private async Task PopulateDropdownsAsync(CourseDetails courseDetails = null)
        {
            // لاحظ أننا نمرر القيمة المختارة (Selected Value) لكل قائمة منسدلة
            ViewData["CourseId"] = new SelectList(await _context.Courses.ToListAsync(), "Id", "Name", courseDetails?.CourseId);
            ViewData["LocationId"] = new SelectList(await _context.Locations.ToListAsync(), "Id", "Name", courseDetails?.LocationId);
            ViewData["CourseTypeId"] = new SelectList(await _context.CourseTypes.ToListAsync(), "Id", "Name", courseDetails?.CourseTypeId);
            ViewData["StatusId"] = new SelectList(await _context.Statuses.ToListAsync(), "Id", "Name", courseDetails?.StatusId);
        }

        private bool CourseDetailsExists(Guid id)
        {
            return _context.CourseDetails.Any(e => e.Id == id);
        }
        // GET: Courses/Delete/5
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
            // يمكنك هنا عرض اسم الدورة لتأكيد الحذف
            // لا حاجة لـ ViewModel معقد هنا، يمكن استخدام الـ Entity مباشرة أو ViewModel بسيط جداً
            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var course = await _context.Courses
                                .Include(c => c.CourseTrainers) // لحذف العلاقات الوسيطة
                                .Include(c => c.CourseDetails) // للنظر في كيفية التعامل مع التفاصيل
                                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            // هنا يجب أن تقرر سياسة الحذف:
            // 1. هل تحذف الـ CourseDetails المرتبطة؟ (Cascade delete إذا تم إعداده في قاعدة البيانات)
            // 2. أو تمنع حذف الدورة إذا كان لديها تفاصيل (CourseDetails)؟
            // 3. أو تجعل CourseId في CourseDetails nullable وتفصلها (أقل شيوعاً هنا)

            // مثال: منع الحذف إذا كان هناك تفاصيل دورات
            if (course.CourseDetails != null && course.CourseDetails.Any())
            {
                // أرسل رسالة خطأ للمستخدم
                TempData["ErrorMessage"] = "لا يمكن حذف هذه الدورة لأنها تحتوي على تفاصيل دورات منفذة. يرجى حذف التفاصيل أولاً.";
                // أعد توجيهه إلى صفحة التفاصيل أو صفحة الحذف مرة أخرى مع الرسالة
                return RedirectToAction(nameof(Delete), new { id = id });
                // أو
                // ModelState.AddModelError("", "لا يمكن حذف هذه الدورة لأنها تحتوي على تفاصيل دورات منفذة. يرجى حذف التفاصيل أولاً.");
                // return View(course); // سيعرض صفحة الحذف مرة أخرى مع رسالة الخطأ
            }

            // حذف علاقات CourseTrainer المرتبطة
            if (course.CourseTrainers != null && course.CourseTrainers.Any())
            {
                _context.CourseTrainers.RemoveRange(course.CourseTrainers);
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "تم حذف الدورة بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(Guid id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }

        // دالة مساعدة لإعادة ملء القوائم المنسدلة وقائمة المدربين في حالة فشل التحقق من صحة النموذج في Edit (POST)
        private async Task PopulateEditViewModelDropdownsAndTrainers(CourseFormViewModel viewModel)
        {
            viewModel.CourseClassifications = (await _context.CourseClassifications
     .OrderBy(c => c.Name)
     .Select(c => new SelectListItem
     {
         Value = c.Id.ToString(),
         Text = c.Name,
         Selected = (c.Id == viewModel.CourseClassificationId)
     })
     .ToListAsync());

            viewModel.CourseClassifications.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- اختر تصنيف الدورة --",
                Selected = (viewModel.CourseClassificationId == Guid.Empty)
            });
            viewModel.Levels = (await _context.Levels
      .OrderBy(l => l.Name)
      .Select(l => new SelectListItem
      {
          Value = l.Id.ToString(),
          Text = l.Name,
          Selected = (l.Id == viewModel.LevelId)
      })
      .ToListAsync());

            viewModel.Levels.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- اختر المستوى --",
                Selected = (viewModel.LevelId == Guid.Empty)
            });
            viewModel.CourseParents = (await _context.CourseParent
    .OrderBy(cp => cp.Name)
    .Select(cp => new SelectListItem
    {
        Value = cp.Id.ToString(),
        Text = cp.Name,
        Selected = (cp.Id == viewModel.CourseParentId)
    })
    .ToListAsync());

            viewModel.CourseParents.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- اختر الدورة المرجعية --",
                Selected = (!viewModel.CourseParentId.HasValue || viewModel.CourseParentId == Guid.Empty)
            });

            var allTrainers = await _context.Trainers.OrderBy(t => t.ArName).ToListAsync();
            viewModel.AvailableTrainers = allTrainers.Select(t => new TrainerCheckboxViewModel
            {
                Id = t.Id,
                Name = t.ArName, // افترض أن Trainer لديه Name
                IsSelected = viewModel.SelectedTrainerIds != null && viewModel.SelectedTrainerIds.Contains(t.Id)
            }).ToList();
        }


        // (داخل CourseDetailsController.cs)
        // GET: CourseDetails/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var courseDetails = await _context.CourseDetails
                .Include(cd => cd.Location)
                .Include(cd => cd.CourseType)
                .Include(cd => cd.Status)
                .Include(cd => cd.CourseTrainees)
                    .ThenInclude(ct => ct.Trainee)
                .Include(cd => cd.Course) // *** جلب الكورس الرئيسي (المنهج) ***
                    .ThenInclude(c => c.CourseClassification) // تصنيف الكورس الرئيسي
                .Include(cd => cd.Course)
                    .ThenInclude(c => c.Level) // مستوى الكورس الرئيسي
                .Include(cd => cd.Course)
                    .ThenInclude(c => c.CourseParent) // الدورة الأم للكورس الرئيسي
                .Include(cd => cd.Course)
                    .ThenInclude(c => c.CourseTrainers) // *** المدربون المعينون للكورس الرئيسي ***
                        .ThenInclude(coursetrainer => coursetrainer.Trainer) // لجلب أسماء المدربين
                .FirstOrDefaultAsync(m => m.Id == id);

            if (courseDetails == null)
            {
                return NotFound();
            }

            var viewModel = new CourseDetailsDisplayViewModel
            {
                // بيانات CourseDetails الحالية
                Course = courseDetails.Course, // جلب الكورس الرئيسي
                Id = courseDetails.Id,
                
                StartDate = courseDetails.StartDate,
                EndDate = courseDetails.EndDate,
                DurationHours = courseDetails.DurationHours,
                LocationName = courseDetails.Location?.Name ?? "N/A",
                CourseTypeName = courseDetails.CourseType?.Name ?? "N/A",
                StatusName = courseDetails.Status?.Name ?? "N/A",

                // بيانات Course الرئيسي
                ParentCourseId = courseDetails.CourseId,

                // المتدربون المسجلون في هذا الـ CourseDetails
                EnrolledTrainees = courseDetails.CourseTrainees.Select(ct => new EnrolledTraineeViewModel
                {
                    ProfileImgeUrl= ct.Trainee?.ProfileImageUrl, 
                    CourseTraineeId = ct.Id,
                    TraineeId = ct.TraineeId,
                    TraineeName = ct.Trainee?.ArName ?? "N/A",
                    TraineeEmail = ct.Trainee?.Email,
                    AttendancePercentage = ct.AttendancePercentage,
                    Grade = ct.Grade,
                    CertificateNumber = ct.CertificateNumber,
                    CertificateIssueDate = ct.CertificateIssueDate
                }).OrderBy(t => t.TraineeName).ToList()
            };

            // تعديل عنوان الصفحة ليشمل اسم الكورس الرئيسي إذا كان مختلفاً
            ViewData["Title"] = $"تفاصيل تنفيذ: {courseDetails.Course?.Name ?? "دورة"} (تبدأ: {courseDetails.StartDate:dd-MM-yyyy})";

            return View(viewModel);
        }
        
        
        public async Task<IActionResult> EnrollTrainee(Guid courseDetailsId)
       
        {
            var courseDetails = await _context.CourseDetails
                                            .Include(cd => cd.Course)
                                            .FirstOrDefaultAsync(cd => cd.Id == courseDetailsId);
            if (courseDetails == null)
            {
                return NotFound();
            }


            // جلب المتدربين الذين لم يتم تسجيلهم بعد في هذه الدورة المنفذة
            var enrolledTraineeIds = await _context.CourseTrainees
                                            .Where(ct => ct.CourseDetailsId == courseDetailsId)
                                            .Select(ct => ct.TraineeId)
                                            .ToListAsync();

            var availableTrainees = await _context.Trainees
                                            .Where(t => !enrolledTraineeIds.Contains(t.Id)) // استبعاد المسجلين
                                            .OrderBy(t => t.ArName)
                                            .ToListAsync();

            var viewModel = new EnrollTraineeViewModel
            {
                CourseId = courseDetails.CourseId,
                CourseDetailsId = courseDetailsId,
                CourseDetailsName = $"{courseDetails.Course.Name} (تبدأ: {courseDetails.StartDate:dd-MM-yyyy})",
                AvailableTrainees = new SelectList(availableTrainees, "Id", "ArName") // افترض أن Trainee لديه Name
            };

            return View("EnrollTrainee", viewModel); // ستحتاج لإنشاء View لهذا
        }

        // POST: /EnrollTrainee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollTrainee(EnrollTraineeViewModel viewModel)
        {
            //if (ModelState.IsValid)
            //{
            // تحقق مرة أخرى أن المتدرب ليس مسجلاً بالفعل (حماية إضافية)
            bool alreadyEnrolled = await _context.CourseTrainees
                .AnyAsync(ct => ct.CourseDetailsId == viewModel.CourseDetailsId && ct.TraineeId == viewModel.SelectedTraineeId);

            if (alreadyEnrolled)
            {
                ModelState.AddModelError("SelectedTraineeId", "هذا المتدرب مسجل بالفعل في هذه الدورة.");
            }
            else
            {
                var courseTrainee = new CourseTrainee
                {
                    CourseDetailsId = viewModel.CourseDetailsId,
                    TraineeId = viewModel.SelectedTraineeId,
                    Notes = viewModel.Notes
                    // يمكنك تعيين قيم افتراضية أخرى هنا إذا أردت
                };
                _context.Add(courseTrainee);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "تم تسجيل المتدرب بنجاح!";
                return RedirectToAction(nameof(Details), new { id = viewModel.CourseDetailsId });
            }
            //}

            // إذا فشل التحقق أو كان مسجلاً بالفعل، أعد ملء القوائم
            var courseDetails = await _context.CourseDetails.Include(cd => cd.Course).FirstOrDefaultAsync(cd => cd.Id == viewModel.CourseDetailsId);
            if (courseDetails != null)
            {
                viewModel.CourseDetailsName = $"{courseDetails.Course.Name} (تبدأ: {courseDetails.StartDate:dd-MM-yyyy})";
            }
            var enrolledTraineeIds = await _context.CourseTrainees.Where(ct => ct.CourseDetailsId == viewModel.CourseDetailsId).Select(ct => ct.TraineeId).ToListAsync();
            var availableTrainees = await _context.Trainees.Where(t => !enrolledTraineeIds.Contains(t.Id)).OrderBy(t => t.ArName).ToListAsync();
            viewModel.AvailableTrainees = new SelectList(availableTrainees, "Id", "Name", viewModel.SelectedTraineeId);

            return View(viewModel);
        }

        // GET: /EditEnrollment/{courseTraineeId}
        public async Task<IActionResult> EditEnrollment(Guid courseTraineeId)
        {
            var courseTrainee = await _context.CourseTrainees
                                        .Include(ct => ct.Trainee)
                                        .Include(ct => ct.CourseDetails)
                                            .ThenInclude(cd => cd.Course)
                                        .FirstOrDefaultAsync(ct => ct.Id == courseTraineeId);

            if (courseTrainee == null)
            {
                return NotFound();
            }

            var viewModel = new EditEnrollmentViewModel
            {
                CourseTraineeId = courseTrainee.Id,
                CourseDetailsId = courseTrainee.CourseDetailsId,
                CourseName = courseTrainee.CourseDetails.Course?.Name ?? "N/A",
                TraineeName = courseTrainee.Trainee?.ArName ?? "N/A",
                AttendancePercentage = courseTrainee.AttendancePercentage,
                Grade = courseTrainee.Grade,
                CertificateNumber = courseTrainee.CertificateNumber,
                CertificateUrl = courseTrainee.CertificateUrl,
                CertificateIssueDate = courseTrainee.CertificateIssueDate,
                Notes = courseTrainee.Notes
            };
            return View(viewModel); // ستحتاج لإنشاء View لهذا
        }

        // POST: /EditEnrollment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEnrollment(EditEnrollmentViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var courseTraineeToUpdate = await _context.CourseTrainees
      .Include(ct => ct.CourseDetails) // هذا الجزء هو الذي يقوم بالـ Include
      .FirstOrDefaultAsync(ct => ct.Id == viewModel.CourseTraineeId); // نبحث عن الكيان المطلوب باستخدام الشرط
                if (courseTraineeToUpdate == null)
                {
                    return NotFound();
                }

                courseTraineeToUpdate.AttendancePercentage = viewModel.AttendancePercentage;
                courseTraineeToUpdate.Grade = viewModel.Grade;
                courseTraineeToUpdate.CertificateNumber = viewModel.CertificateNumber;
                courseTraineeToUpdate.CertificateUrl = viewModel.CertificateUrl;
                courseTraineeToUpdate.CertificateIssueDate = viewModel.CertificateIssueDate;
                courseTraineeToUpdate.Notes = viewModel.Notes;
                // UpdateDate, etc.

                try
                {


                    _context.Update(courseTraineeToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "تم تحديث بيانات تسجيل المتدرب بنجاح!";
                    return RedirectToAction(nameof(Details), new { id = courseTraineeToUpdate.CourseDetails.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    // معالجة خطأ التزامن
                    ModelState.AddModelError("", "لم يتم حفظ البيانات. ربما تم تعديلها بواسطة مستخدم آخر.");
                }
            }
            // إذا فشل، أعد ملء بعض البيانات للعرض
            var courseTrainee = await _context.CourseTrainees
                                        .Include(ct => ct.Trainee)
                                        .Include(ct => ct.CourseDetails)
                                        .ThenInclude(cd => cd.Course)
                                        .AsNoTracking() // مهم عند إعادة عرض الفورم بعد فشل التحديث
                                        .FirstOrDefaultAsync(ct => ct.Id == viewModel.CourseTraineeId);
            if (courseTrainee != null)
            {
                viewModel.CourseName = courseTrainee.CourseDetails.Course?.Name ?? "N/A";
                viewModel.TraineeName = courseTrainee.Trainee?.ArName ?? "N/A";
            }
            return View(viewModel);
        }


        // POST: /RemoveEnrollment/{courseTraineeId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveEnrollment(Guid courseTraineeId) // قد تستقبل CourseDetailsId أيضاً للredirect
        {
            var courseTrainee = await _context.CourseTrainees.FindAsync(courseTraineeId);
            if (courseTrainee == null)
            {
                return NotFound();
            }

            var courseDetailsId = courseTrainee.CourseDetailsId; // احفظ الـ ID للـ redirect

            var CourseDetails = await _context.CourseDetails.FindAsync(courseDetailsId);


            _context.CourseTrainees.Remove(courseTrainee);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم إلغاء تسجيل المتدرب بنجاح.";
            return RedirectToAction(nameof(Details), new { id = CourseDetails.CourseId });
        }

        public async Task<IActionResult> PrintCertificate(Guid id) 
        {
            if (id == Guid.Empty)
            {
                return BadRequest("معرف التسجيل مطلوب.");
            }

            var courseTrainee = await _context.CourseTrainees
                .Include(ct => ct.Trainee) // لجلب اسم المتدرب وبياناته
                .Include(ct => ct.CourseDetails) // لجلب تفاصيل تنفيذ الدورة
                    .ThenInclude(cd => cd.Course) // لجلب اسم الدورة الرئيسي وبياناتها
                .Include(ct => ct.CourseDetails)
                    .ThenInclude(cd => cd.Location) // إذا كان الموقع جدول منفصل
                .FirstOrDefaultAsync(ct => ct.Id == id);

            if (courseTrainee == null)
            {
                return NotFound("بيانات التسجيل غير موجودة.");
            }

            if (courseTrainee.Trainee == null || courseTrainee.CourseDetails == null || courseTrainee.CourseDetails.Course == null)
            {
                // هذا يعني أن بعض البيانات المرتبطة مفقودة، يجب معالجة هذا
                return BadRequest("بيانات مرتبطة بالدورة أو المتدرب مفقودة.");
            }

            // تحويل Grade الرقمي إلى نصي إذا لزم الأمر
            string gradeText = "غير محدد";
            if (courseTrainee.Grade.HasValue)
            {
                // يمكنك هنا وضع منطق لتحويل الدرجة الرقمية إلى وصف
                // مثال بسيط:
                if (courseTrainee.Grade >= 90) gradeText = "ممتاز";
                else if (courseTrainee.Grade >= 80) gradeText = "جيد جداً";
                else if (courseTrainee.Grade >= 70) gradeText = "جيد";
                else if (courseTrainee.Grade >= 60) gradeText = "مقبول";
                else gradeText = "لم يجتز"; // أو اتركه كرقم courseTrainee.Grade.Value.ToString("0.##")
            }


            var viewModel = new CertificateViewModel
            {
                TraineeName = courseTrainee.Trainee.ArName, // افترض أن لديك خاصية FullName في Trainee
                CourseName = courseTrainee.CourseDetails.Course.Name,
                CourseStartDate = courseTrainee.CourseDetails.StartDate,
                CourseEndDate = courseTrainee.CourseDetails.EndDate,
                CourseDurationHours = courseTrainee.CourseDetails.DurationHours,
                CourseLocation = courseTrainee.CourseDetails.Location?.Name ?? "غير محدد", // افترض أن Location له Name
                CertificateNumber = courseTrainee.CertificateNumber,
                CertificateIssueDate = courseTrainee.CertificateIssueDate ?? DateTime.Now, // تاريخ اليوم كافتراضي إذا لم يحدد
                Grade = gradeText, // أو courseTrainee.Grade?.ToString("0.##") إذا أردت عرض الرقم

                // يمكنك تخصيص النصوص الثابتة هنا إذا أردت، أو تركها للقيم الافتراضية في ViewModel
                // IssuingOrganizationName = "اسم مؤسستك",
                // LogoUrl = "/path/to/your/logo.png",
                // StampUrl = "/path/to/your/stamp.png"
            };

            // اسم الـ View سيكون PrintCertificate.cshtml
            return View("PrintCertificate", viewModel);
        }







        public async Task<IActionResult> CourseDetailsPdfView(Guid id) // id هو CourseDetailsId
        {
            var courseDetails = await _context.CourseDetails
                .Include(cd => cd.Course)
                    .ThenInclude(c => c.CourseClassification)
                .Include(cd => cd.Course)
                    .ThenInclude(c => c.Level)
                .Include(cd => cd.Location)
                .Include(cd => cd.CourseType)
                .Include(cd => cd.Status)
                .Include(cd => cd.CourseTrainees)
                    .ThenInclude(ct => ct.Trainee)
                .FirstOrDefaultAsync(cd => cd.Id == id);

            if (courseDetails == null)
            {
                return NotFound();
            }

            var viewModel = new CourseDetailsDisplayViewModel // استخدم نفس الـ ViewModel
            {
                Id = courseDetails.Id,
                ParentCourseId = courseDetails.CourseId,
                StartDate = courseDetails.StartDate,
                EndDate = courseDetails.EndDate,
                DurationHours = courseDetails.DurationHours,
                LocationName = courseDetails.Location?.Name ?? "غير محدد",
                CourseTypeName = courseDetails.CourseType?.Name ?? "غير محدد",
                StatusName = courseDetails.Status.Name ?? "غير محدد",
                CourseName = courseDetails.Course.Name, // اسم الكورس الرئيسي
                                                        // تأكد من تعبئة هذه الخصائص إذا كنت ستعرضها في الـ PDF View
                Course = new Course 
                {
                    Name = courseDetails.Course.Name, // مكرر، اختر واحداً
                    Code = courseDetails.Course.Code,
                    Description = courseDetails.Course.Description,
                    CourseClassification = new CourseClassification { Name = courseDetails.Course.CourseClassification?.Name ?? "غير محدد" },
                    Level = new Level { Name = courseDetails.Course.Level?.Name ?? "غير محدد" }
                },
                EnrolledTrainees = courseDetails.CourseTrainees.Select(ct => new EnrolledTraineeViewModel
                {
                    ProfileImgeUrl= ct.Trainee?.ProfileImageUrl, // افترض أن لديك خاصية ProfileImageUrl في Trainee
                    CourseTraineeId = ct.Id,
                    TraineeId = ct.TraineeId,
                    TraineeName = ct.Trainee.EnName, // افترض أن لديك FullName
                    TraineeEmail = ct.Trainee.Email,
                    AttendancePercentage = ct.AttendancePercentage,
                    Grade = ct.Grade,
                    CertificateNumber = ct.CertificateNumber,
                    CertificateIssueDate = ct.CertificateIssueDate
                }).ToList()
            };



            return View(viewModel);
        }
    }






}
