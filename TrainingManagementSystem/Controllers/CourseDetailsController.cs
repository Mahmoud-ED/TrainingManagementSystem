using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Migrations;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.ViewModels;

namespace TrainingManagementSystem.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    [Authorize(Roles = "Admin,Prog")]
    [Display(Name = "البرامج التدريبية")] // ✅ Controller title

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
            // ================== [جديد] بداية منطق التحديث التلقائي للحالات ==================

            var inPlanStatusId = new Guid("07057281-82e6-4789-a740-e30d1022d4f6");          // مستهدفة
            var inRegistrationStatusId = new Guid("07057281-82e6-4789-a740-e30d1029d4f6");  // قيد التسجيل
            var inExecutionStatusId = new Guid("07057281-82e6-4789-a740-e30d1129d4f6");     // قيد التنفيذ
            var notExecutedStatusId = new Guid("07057281-82e6-4789-a740-e31d1129d4f6");     // لم يتم التنفيذ

            // 2. جلب كل الدورات التي لم تبدأ بعد ولكن حان وقتها
            // (حالتها إما "مستهدفة" أو "قيد التسجيل" وتاريخ بدئها قد حان أو فات)
            var statusesToReview = new List<Guid> { inPlanStatusId, inRegistrationStatusId };

            var coursesToReview = await _context.CourseDetails
                .Include(cd => cd.CourseTrainees) // مهم لجلب عدد المسجلين
                .Where(cd => cd.StartDate <= DateTime.Today)
                .ToListAsync();

            bool hasChanges = false;

            // 3. المرور على كل دورة وتطبيق المنطق الصحيح
            foreach (var course in coursesToReview)
            {
                // التحقق من وجود مسجلين
                bool hasEnrollments = course.CourseTrainees.Any();

                // السيناريو الأول: الدورة حان وقتها ولا يوجد بها مسجلون
                // (تطبق على حالتي "مستهدفة" و "قيد التسجيل")
                if (!hasEnrollments)
                {
                    course.StatusId = notExecutedStatusId;
                    hasChanges = true;
                }
                // السيناريو الثاني: الدورة حان وقتها، ويوجد مسجلون، وحالتها كانت "قيد التسجيل"
                else if (hasEnrollments && course.StatusId == inRegistrationStatusId)
                {
                    course.StatusId = inExecutionStatusId;
                    hasChanges = true;
                }
            }

            // 4. حفظ التغييرات في قاعدة البيانات فقط إذا كان هناك تغييرات
            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }

            var courseDetailsList = await _context.CourseDetails
                                        .Include(cd => cd.Course)
                                        .ThenInclude(cd => cd.CourseClassification)
                                        .Include(cd => cd.Locations)
                                        .Include(cd => cd.CourseType)
                                        .Include(cd => cd.Status)
                                        .Include(cd => cd.CourseTrainees)
                                        .OrderByDescending(cd => cd.Created)
                                        .ToListAsync();

            var upcomingCourses = courseDetailsList
                                    .Where(cd => cd.Status?.Name == "Open" && cd.StartDate >= DateTime.Today)
                                    .OrderByDescending(cd => cd.Created)
                                    .ToList();

            var viewModel = new CourseDetailsIndexViewModel
            {
                CourseDetailsList = courseDetailsList,
                ActiveCoursesCount = courseDetailsList.Count(cd => cd.Status?.Name == "Open"),
                CompletedCoursesCount = courseDetailsList.Count(cd => cd.Status?.Name == "Close"),
                PostponedCoursesCount = courseDetailsList.Count(cd => cd.Status?.Name == "Next"),
                CancelledCoursesCount = courseDetailsList.Count(cd => cd.Status?.Name == "Cansel"),
                UpcomingCoursesCount = upcomingCourses.Count,
                NextUpcomingCourse = upcomingCourses.FirstOrDefault(),
                TotalRegisteredTrainees = courseDetailsList.Sum(cd => cd.CourseTrainees?.Count ?? 0)
            };

            return View(viewModel);
        }
        public async Task<IActionResult> AnnualPlan()
        {
            // 1. تحديد السنة الحالية
            int year = DateTime.Today.Year;

            // 2. جلب جميع تفاصيل الدورات للسنة المحددة
            var allCourseDetails = await _context.CourseDetails
                                        .Include(cd => cd.Course)
                                        .Include(cd => cd.Locations)
                                        .Include(cd => cd.Status)
                                        .Where(cd => cd.StartDate.Year == year) // فلترة حسب السنة الحالية
                                        .OrderByDescending(cd => cd.Created) // ترتيب حسب تاريخ البدء
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
        public async Task<IActionResult> Create(Guid? id)
        {

            var course1 = await _context.Courses
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
              .Include(c => c.CourseDetails)
                  .ThenInclude(ct => ct.CoursDetailsTrainer)
                  .ThenInclude(nt => nt.Trainer)
              .FirstOrDefaultAsync(m => m.Id == id);


            var courseList = await _context.Courses
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
                .ThenInclude(ct => ct.Trainer).ToListAsync();


            var viewModel = new CourseFormViewModel
            {
                Course = course1,
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

            if (viewModel.Id==null)
            {
              
                await PopulateFormViewModelDropdowns(viewModel);
                ModelState.AddModelError("Create", "يجب اختيار تصنيف");
                return View("Create", viewModel);
            }

            // الجديد: إضافة تفاصيل الدورة (CourseDetails)
            if (viewModel.CourseDetailsEntries != null)
            {
                foreach (var entryVm in viewModel.CourseDetailsEntries)
                {
                    // تحقق إضافي لكل عنصر إذا لزم الأمر
                    if (entryVm.DurationHours > 0) // مثال على تحقق بسيط
                    {
                        var name = _context.Courses.FirstOrDefault(Course => Course.Id == viewModel.Id)?.Name;
                        var courseDetail = new CourseDetails
                        {
                            Created = DateTime.Now, // تعيين تاريخ الإنشاء    ss
                            DurationHours = entryVm.DurationHours,
                            StartDate = entryVm.StartDate,
                            EndDate = entryVm.EndDate,
                            Name = name,
                            LocationId = entryVm.LocationId,
                            CourseTypeId = null,
                            StatusId = entryVm.StatusId,
                            Numberoftargets = entryVm.Numberoftargets, // نقل عدد الأهداف من الفورم الرئيسي
                            CourseId = viewModel.Id, // ربط الدورة بالتفاصيل
                        };
                        _context.CourseDetails.Add(courseDetail);
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "تم إنشاء الدورة وتفاصيلها بنجاح!";
            return RedirectToAction(nameof(Index));

        }

        // في CoursesController.cs أو الكنترولر المسؤول
        [HttpGet]
        public async Task<IActionResult> GetCourseDetailEntryPartial(int index)
        {
            // === التغيير هنا: جلب القائمة مع FullCount ===
            // بدلاً من تحويلها إلى SelectListItem مباشرة، نمرر كائنًا يحتوي على البيانات المطلوبة
            var locationsWithData = await _context.Locations
                                        .OrderBy(l => l.Name)
                                        .Select(l => new { Id = l.Id.ToString(), l.Name, l.FullCount }) // جلب البيانات المطلوبة
                                        .ToListAsync();

            var parentViewModelForDropdowns = new CourseFormViewModel
            {
             

                Statuses = (await _context.Statuses
                    .OrderBy(s => s.Name)
                    .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                    .ToListAsync()),

                Locations = (await _context.Locations
                    .OrderBy(ct => ct.Name)
                    .Select(ct => new SelectListItem { Value = ct.Id.ToString(), Text = ct.Name })
                    .ToListAsync()),
            };

            // إضافة عنصر اختياري فارغ
            parentViewModelForDropdowns.CourseTypes.Insert(0, new SelectListItem { Value = "", Text = "-- اختر نوع الدورة --" });
            parentViewModelForDropdowns.Statuses.Insert(0, new SelectListItem { Value = "", Text = "-- اختر الحالة --" });
            parentViewModelForDropdowns.Locations.Insert(0, new SelectListItem { Value = "", Text = "-- اختر المكان --" });


         
            // === التغيير هنا: تمرير قائمة المواقع الجديدة عبر ViewData ===
            ViewData["LocationsWithData"] = locationsWithData;

            ViewData["index"] = index;
            ViewData["ParentViewModel"] = parentViewModelForDropdowns;
            ViewData["ParentListSize"] = new List<CourseDetailFormEntryViewModel>();

            return PartialView("_CourseDetailFormEntry", new CourseDetailFormEntryViewModel { StartDate = DateTime.Today });
        }

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

       

            viewModel.Statuses.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- اختر الحالة --"
            });

            // إذا كنت تريد إعادة ملء قيم CourseDetailsEntries عند الفشل، فهذا أكثر تعقيداً
            // لأنك تحتاج إلى الحفاظ على القيم التي أدخلها المستخدم.
            // الـ Model Binding سيفعل ذلك تلقائياً إذا كانت أسماء الحقول صحيحة.
        }

        private async Task PopulateDropdownsAsync(CourseFormViewModel viewModel)
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

            var allTrainers = await _context.Trainers.OrderBy(t => t.ArName).ToListAsync();
            viewModel.AvailableTrainers = allTrainers.Select(t => new TrainerCheckboxViewModel
            {
                Id = t.Id,
                Name = t.ArName,
                IsSelected = viewModel.SelectedTrainerIds != null && viewModel.SelectedTrainerIds.Contains(t.Id)
            }).ToList();
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
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,CourseId,DurationHours,StartDate,EndDate,LocationId,StatusId,Numberoftargets")] CourseDetails courseDetails)
        {
            if (id != courseDetails.Id)
            {
                return NotFound();
            }

                try
                {
                    courseDetails.Modified = DateTime.Now; // تعيين تاريخ التعديل
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
                return RedirectToAction(nameof(Index));
        
        }

        // دالة مساعدة لتجنب تكرار الكود
        private async Task PopulateDropdownsAsync(CourseDetails courseDetails = null)
        {
            // لاحظ أننا نمرر القيمة المختارة (Selected Value) لكل قائمة منسدلة
            ViewData["CourseId"] = new SelectList(await _context.Courses.ToListAsync(), "Id", "Name", courseDetails?.CourseId);
            ViewData["LocationId"] = new SelectList(await _context.Locations.ToListAsync(), "Id", "Name", courseDetails?.LocationId);
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

            var courseDetails = await _context.CourseDetails
                .Include(cd => cd.Locations)
                .Include(cd => cd.CourseType)
                .Include(cd => cd.Status)
                .Include(cd => cd.CourseTrainees)
                    .ThenInclude(ct => ct.Trainee)
                         .Include(cd => cd.CoursDetailsTrainer)
                    .ThenInclude(ct => ct.Trainer)
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
                Name = courseDetails.Name,

                ClasifcationName = courseDetails.Course?.CourseClassification?.Name ?? "N/A",
                Numberoftargets = courseDetails.Numberoftargets,
                DurationHours = courseDetails.DurationHours,
                LocationName = courseDetails.Locations?.Name ?? "N/A",
                CourseTypeName = "N/A",
                StatusName = courseDetails.Status?.Name ?? "N/A",
                Trainers = courseDetails.CoursDetailsTrainer.Select(ct => new Trainer
                {
                    Id = ct.Trainer.Id,
                    ArName = ct.Trainer.ArName,
                    State = ct.Trainer.State,
                    ProfileImageUrl = ct.Trainer.ProfileImageUrl
                }).ToList(),

                // بيانات Course الرئيسي
                ParentCourseId = courseDetails.CourseId,

                // المتدربون المسجلون في هذا الـ CourseDetails
                EnrolledTrainees = courseDetails.CourseTrainees.Select(ct => new EnrolledTraineeViewModel
                {
                    ProfileImgeUrl = ct.Trainee?.ProfileImageUrl,
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
            return View(viewModel);
        
        
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var courseDetails = await _context.CourseDetails.FindAsync(id);

            if (courseDetails == null)
            {
                TempData["ErrorMessage"] = "لم يتم العثور على سجل الدورة. ربما تم حذفه بالفعل.";
                return RedirectToAction(nameof(Index)); // أو أي صفحة قائمة مناسبة
            }

            try
            {
                // 3. تنفيذ عملية الحذف
                _context.CourseDetails.Remove(courseDetails);

                // 4. حفظ التغييرات في قاعدة البيانات
                await _context.SaveChangesAsync();

                // 5. إرسال رسالة نجاح للمستخدم عبر TempData
                TempData["SuccessMessage"] = $"تم حذف سجل تنفيذ الدورة '{courseDetails.Name}' بنجاح.";

                // 6. إعادة توجيه المستخدم إلى صفحة القائمة الرئيسية
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {

                TempData["ErrorMessage"] = "حدث خطأ أثناء محاولة حذف السجل. قد يكون مرتبطًا ببيانات أخرى لا يمكن حذفها.";

                // إعادة المستخدم إلى صفحة تأكيد الحذف مرة أخرى مع عرض الخطأ
                return RedirectToAction(nameof(Delete), new { id = id });
            }
        }

        [HttpPost, ActionName("DeleteTrainer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTrainer(Guid CoursDetailsid ,Guid trainer)
        {
            var courseDetailsTrainer = await _context.CoursDetailsTrainer.Where(CoursDetailsTrainer => CoursDetailsTrainer.CourseDetailsId == CoursDetailsid && CoursDetailsTrainer.TrainerId == trainer).FirstOrDefaultAsync();

            if (courseDetailsTrainer == null)
            {
                TempData["ErrorMessage"] = "لم يتم العثور على سجل المدرب او المشرف. ربما تم حذفه بالفعل.";
                return RedirectToAction("Details", new { id = CoursDetailsid });
            }

            try
            {
                _context.CoursDetailsTrainer.Remove(courseDetailsTrainer);

                await _context.SaveChangesAsync();

                return RedirectToAction("Details", new { id = CoursDetailsid });
            }
            catch (DbUpdateException ex)
            {

                TempData["ErrorMessage"] = "حدث خطأ أثناء محاولة حذف السجل. قد يكون مرتبطًا ببيانات أخرى لا يمكن حذفها.";

                return RedirectToAction("Details", new { id = CoursDetailsid });
            }
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var courseDetails = await _context.CourseDetails
                .Include(cd => cd.Locations)
                .Include(cd => cd.CourseType)
                .Include(cd => cd.Status)
                .Include(cd => cd.CourseTrainees)
                    .ThenInclude(ct => ct.Trainee)
                         .Include(cd => cd.CoursDetailsTrainer)
                    .ThenInclude(ct => ct.Trainer)
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
                Name = courseDetails.Name,

                ClasifcationName= courseDetails.Course?.CourseClassification?.Name ?? "N/A",
                Numberoftargets = courseDetails.Numberoftargets,
                DurationHours = courseDetails.DurationHours,
                LocationName = courseDetails.Locations?.Name ?? "N/A",
                CourseTypeName = "N/A",
                StatusName = courseDetails.Status?.Name ?? "N/A",
                Trainers = courseDetails.CoursDetailsTrainer.Select(ct => new Trainer
                {
                    Id = ct.Trainer.Id,
                    ArName = ct.Trainer.ArName,
                    State = ct.Trainer.State,
                    ProfileImageUrl = ct.Trainer.ProfileImageUrl
                }).ToList(),

                // بيانات Course الرئيسي
                ParentCourseId = courseDetails.CourseId,

                // المتدربون المسجلون في هذا الـ CourseDetails
                EnrolledTrainees = courseDetails.CourseTrainees.Select(ct => new EnrolledTraineeViewModel
                {
                    ProfileImgeUrl = ct.Trainee?.ProfileImageUrl,
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
            ViewData["Title"] = $"تفاصيل تنفيذ: {courseDetails.Course?.Name ?? "دورة"} (تبدأ: {courseDetails.StartDate:dd-MM-yyyy})";

            return View(viewModel);
        }

        [HttpGet("CourseDetails/EnrollMultiple/{courseDetailsId}")]
        public async Task<IActionResult> EnrollMultiple(Guid courseDetailsId)
        {
            var courseDetails = await _context.CourseDetails
                                            .AsNoTracking()
                                            .Include(cd => cd.Course)
                                            .FirstOrDefaultAsync(cd => cd.Id == courseDetailsId);

            if (courseDetails == null)
            {
                return NotFound("تفاصيل الدورة المطلوبة غير موجودة.");
            }

            var viewModel = new EnrollMultipleTraineesViewModel
            {
                CourseDetailsId = courseDetails.Id,
                CourseDetailsName = $"{courseDetails.Course.Name} (تبدأ: {courseDetails.StartDate:dd-MM-yyyy})"
            };

            // استدعاء الدالة المساعدة لملء كل البيانات الديناميكية والمنطق المعقد
            await PopulateViewModelData(viewModel, courseDetailsId);

            return View("EnrollMultiple", viewModel);
        }

        // POST: /Enrollment/EnrollMultiple
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollMultiple(EnrollMultipleTraineesViewModel viewModel)
        {
            if (viewModel.SelectedTraineeIds == null || !viewModel.SelectedTraineeIds.Any())
            {
                ModelState.AddModelError(nameof(viewModel.SelectedTraineeIds), "يجب اختيار متدرب واحد على الأقل.");
            }

            //if (ModelState.IsValid)
            //{
                var alreadyEnrolledIds = await _context.CourseTrainees
                    .Where(ct => ct.CourseDetailsId == viewModel.CourseDetailsId && viewModel.SelectedTraineeIds.Contains(ct.TraineeId))
                    .Select(ct => ct.TraineeId)
                    .ToListAsync();

                var newIdsToEnroll = viewModel.SelectedTraineeIds.Except(alreadyEnrolledIds).ToList();

                if (newIdsToEnroll.Any())
                {
                    var newEnrollments = newIdsToEnroll.Select(traineeId => new CourseTrainee
                    {
                        CourseDetailsId = viewModel.CourseDetailsId,
                        TraineeId = traineeId,
                        Created = DateTime.UtcNow
                    }).ToList();

                    _context.CourseTrainees.AddRange(newEnrollments);


                var targetedStatusId = new Guid("07057281-82e6-4789-a740-e30d1022d4f6"); // حالة "مستهدفة"
                var underRegistrationStatusId = new Guid("07057281-82e6-4789-a740-e30d1029d4f6"); // حالة "قيد التسجيل"

                // 2. جلب الدورة نفسها من قاعدة البيانات للتحقق من حالتها وتحديثها
                var courseToUpdate = await _context.CourseDetails.FindAsync(viewModel.CourseDetailsId);

                // تأكد من أن الدورة موجودة
                if (courseToUpdate == null)
                {
                    return NotFound("البرنامج التدريبي المحدد غير موجود.");
                }

                // 3. التحقق من عدد المسجلين حالياً في الدورة
                var currentEnrollmentCount = await _context.CourseTrainees
                    .CountAsync(ct => ct.CourseDetailsId == viewModel.CourseDetailsId);

                // 4. تطبيق الشرط: إذا كان عدد المسجلين صفر (0) وكانت حالة الدورة هي "مستهدفة"
                if (currentEnrollmentCount == 0  )
                {
                    // قم بتغيير حالة الدورة إلى "قيد التسجيل"
                    courseToUpdate.StatusId = underRegistrationStatusId;
                }


                await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"تم تسجيل عدد ({newEnrollments.Count}) متدرب بنجاح!";
                }
                else
                {
                    TempData["WarningMessage"] = "لم يتم تحديد أي متدربين جدد للتسجيل أو أنهم مسجلون بالفعل.";
                }

                return RedirectToAction("Details", "CourseDetails", new { id = viewModel.CourseDetailsId });
            }
        
        // POST: /Enrollment/AddAndEnrollTrainee
        [HttpPost]
        public async Task<IActionResult> AddAndEnrollTrainee([FromBody] QuickAddTraineeViewModel model)
        {
         

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var emailExists = await _context.Trainees.AnyAsync(t => t.Email == model.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "هذا البريد الإلكتروني مستخدم بالفعل لمتدرب آخر.");
                return BadRequest(ModelState);
            }

            var newTrainee = new Trainee
            {
                ArName = model.ArName,
                EnName = "", // يمكن تركه فارغًا أو تعديله حسب الحاجة
                Email = model.Email,
                PhoneNo = model.PhoneNo,
                OrganizationId = model.OrganizationId,
                QualificationId = model.QualificationId,
                Address = "", // قيمة افتراضية
                NationalNo = "" // قيمة افتراضية
            };

            _context.Trainees.Add(newTrainee);

            var enrollment = new CourseTrainee
            {
                CourseDetailsId = model.CourseDetailsId,
                Trainee = newTrainee, // ربط مباشر
            };
            _context.CourseTrainees.Add(enrollment);

            await _context.SaveChangesAsync(); // حفظ المتدرب والتسجيل معًا

            var orgName = (await _context.Organizitions.FindAsync(newTrainee.OrganizationId))?.Name ?? "غير محدد";


            var targetedStatusId = new Guid("07057281-82e6-4789-a740-e30d1022d4f6"); // حالة "مستهدفة"
            var underRegistrationStatusId = new Guid("07057281-82e6-4789-a740-e30d1029d4f6"); // حالة "قيد التسجيل"

            // 2. جلب الدورة نفسها من قاعدة البيانات للتحقق من حالتها وتحديثها
            var courseToUpdate = await _context.CourseDetails.FindAsync(model.CourseDetailsId);

            // تأكد من أن الدورة موجودة
            if (courseToUpdate == null)
            {
                return NotFound("البرنامج التدريبي المحدد غير موجود.");
            }

            // 3. التحقق من عدد المسجلين حالياً في الدورة
            var currentEnrollmentCount = await _context.CourseTrainees
                .CountAsync(ct => ct.CourseDetailsId == model.CourseDetailsId);

            // 4. تطبيق الشرط: إذا كان عدد المسجلين صفر (0) وكانت حالة الدورة هي "مستهدفة"
            if (currentEnrollmentCount == 0 && courseToUpdate.StatusId == targetedStatusId)
            {
                // قم بتغيير حالة الدورة إلى "قيد التسجيل"
                courseToUpdate.StatusId = underRegistrationStatusId;
                await _context.SaveChangesAsync(); 
            }





            var result = new
            {
                id = newTrainee.Id,
                name = newTrainee.ArName,
                nationalId = newTrainee.NationalNo,
                organizationId = newTrainee.OrganizationId,
                organizationName = orgName
            };

            return Ok(result);
        }
       
        private async Task PopulateViewModelData(EnrollMultipleTraineesViewModel viewModel, Guid courseDetailsId)
        {
            // --- الخطوة 1: جلب معلومات الكورس الحالي ومتطلباته ---
            var courseDetails = await _context.CourseDetails
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(cd => cd.Id == courseDetailsId);
            if (courseDetails == null) return;

            var prerequisiteCourseIds = await _context.CoursePrerequisites
                .AsNoTracking()
                .Where(p => p.CourseId == courseDetails.CourseId)
                .Select(p => p.PrerequisiteCourseId)
                .ToListAsync();

            // --- الخطوة 2: جلب IDs المتدربين المسجلين حالياً ---
            var enrolledTraineeIds = await _context.CourseTrainees
                .AsNoTracking()
                .Where(ct => ct.CourseDetailsId == courseDetailsId)
                .Select(ct => ct.TraineeId)
                .ToListAsync();

            // --- الخطوة 3: بناء استعلام ديناميكي لجلب المتدربين المؤهلين ---
            var traineesQuery = _context.Trainees.AsNoTracking();

            // 3.1: استبعاد المسجلين حاليًا
            traineesQuery = traineesQuery.Where(t => !enrolledTraineeIds.Contains(t.Id));

            // 3.2: تطبيق فلتر المتطلبات المسبقة إذا كانت موجودة
            if (prerequisiteCourseIds.Any())
            {
                // هذا هو المنطق الأساسي: المتدرب يجب أن يكون قد أكمل "كل" المتطلبات
                traineesQuery = traineesQuery.Where(t =>
                    prerequisiteCourseIds.All(prereqId =>
                        _context.CourseTrainees.Any(ct =>
                            ct.TraineeId == t.Id &&
                            ct.CourseDetails.CourseId == prereqId
                        )
                    )
                );
            }

            // --- الخطوة 4: تنفيذ الاستعلام وتعبئة ViewModel ---
            var availableTrainees = await traineesQuery
                .Include(t => t.Organizition)
                .Select(t => new TraineeSelectionItem
                {
                    Id = t.Id,
                    Name = t.ArName,
                    NationalId = t.NationalNo,
                    OrganizationId = t.OrganizationId,
                    OrganizationName = t.Organizition.Name
                }).ToListAsync();

            viewModel.Trainees = availableTrainees;

            // --- الخطوة 5: تعبئة القوائم المنسدلة ---
            var organizations = await _context.Organizitions.AsNoTracking().OrderBy(o => o.Name).ToListAsync();
            var qualifications = await _context.Qualifications.AsNoTracking().OrderBy(q => q.Name).ToListAsync();

            viewModel.AvailableOrganizations = new SelectList(organizations, "Id", "Name", viewModel.SelectedOrganizationId);
            viewModel.NewTrainee.CourseDetailsId = courseDetailsId;
            viewModel.NewTrainee.AvailableOrganizations = new SelectList(organizations, "Id", "Name");
            viewModel.NewTrainee.AvailableQualifications = new SelectList(qualifications, "Id", "Name");
        }

        // GET: /EditEnrollment/{courseTraineeId}
        [HttpGet]
        public async Task<IActionResult> EditEnrollment(Guid courseTraineeId)
        {
            var courseTrainee = await _context.CourseTrainees
      .Include(ct => ct.CourseDetails)
          .ThenInclude(cd => cd.Course)
      .AsNoTracking()
      .FirstOrDefaultAsync(ct => ct.Id == courseTraineeId);


            if (courseTrainee == null)
            {
                return NotFound();
            }


            decimal calculatedAttendancePercentage = 0;

            // 2. حساب إجمالي الأيام الدراسية التي تم تسجيل حضور لها في هذه الدورة
            // نستخدم `Select` و `Distinct` لنحصل على قائمة فريدة بالتواريخ
            var totalCourseDaysRecorded = await _context.Attendances
                .Where(a => a.CourseDetailsId == courseTrainee.CourseDetailsId)
                .Select(a => a.AttendanceDate.Date)
                .Distinct()
                .CountAsync();

            if (totalCourseDaysRecorded > 0)
            {
                // 3. حساب عدد أيام حضور هذا المتدرب المحدد (حاضر أو متأخر)
                // نعتبر "Present" و "Late" كحضور. "Excused" لا يُحتسب.
                var traineePresentDays = await _context.Attendances
                    .CountAsync(a => a.CourseDetailsId == courseTrainee.CourseDetailsId &&
                                     a.TraineeId == courseTrainee.TraineeId &&
                                     (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late));

                // 4. حساب النسبة المئوية مع التأكد من عدم القسمة على صفر
                calculatedAttendancePercentage = ((decimal)traineePresentDays / totalCourseDaysRecorded) * 100;
            }

            // =============================================================
            //                          <<<--- نهاية الجزء الجديد --->>>
            // =============================================================


            var viewModel = new EditEnrollmentViewModel
            {
                CourseTraineeId = courseTrainee.Id,
                CourseDetailsId = courseTrainee.CourseDetailsId,
                CourseName = courseTrainee.CourseDetails.Course?.Name ?? "N/A",
                TraineeName = courseTrainee.Trainee?.ArName ?? "N/A",

                // هنا نستخدم القيمة المحسوبة بدلاً من القيمة المخزنة
                AttendancePercentage = calculatedAttendancePercentage,

                // باقي الحقول تبقى كما هي، قد تكون معدلة يدويًا سابقًا
                Grade = courseTrainee.Grade,
                CertificateNumber = courseTrainee.CertificateNumber,
                CertificateUrl = courseTrainee.CertificateUrl,
                CertificateIssueDate = courseTrainee.CertificateIssueDate,
                Notes = courseTrainee.Notes
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEnrollment(EditEnrollmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var courseTraineeToUpdate = await _context.CourseTrainees.FindAsync(model.CourseTraineeId);
            if (courseTraineeToUpdate == null)
            {
                return NotFound();
            }

            // تحديث كل الحقول بما فيها نسبة الحضور الجديدة
            courseTraineeToUpdate.AttendancePercentage = model.AttendancePercentage;
            courseTraineeToUpdate.Grade = model.Grade;
            courseTraineeToUpdate.CertificateNumber = model.CertificateNumber;
            courseTraineeToUpdate.CertificateUrl = model.CertificateUrl;
            courseTraineeToUpdate.CertificateIssueDate = model.CertificateIssueDate;
            courseTraineeToUpdate.Notes = model.Notes;

            _context.Update(courseTraineeToUpdate);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"تم تحديث بيانات تسجيل المتدرب '{model.TraineeName}' بنجاح.";
            return RedirectToAction("Details", "CourseDetails", new { id = model.CourseDetailsId });
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

            //var CourseDetails = await _context.CourseDetails.FindAsync(courseDetailsId);


            _context.CourseTrainees.Remove(courseTrainee);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم إلغاء تسجيل المتدرب بنجاح.";
            return RedirectToAction(nameof(Details), new { id = courseDetailsId });
        }

        public async Task<IActionResult> PrintCertificate(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest("معرف التسجيل مطلوب.");
            }

            var IsPased = await _context.CourseTrainees.AnyAsync(ct => ct.Id == id && ct.Grade != null);
            if (!IsPased)
            {
                return BadRequest("لا يمكن طباعة الشهادة لأن المتدرب لم يحصل على درجة.");
            }
            //var courseTrainee = await _context.CourseTrainees
            //    .Include(ct => ct.CourseDetails) // لجلب تفاصيل تنفيذ الدورة
            //        .ThenInclude(cd => cd.Course) // لجلب اسم الدورة الرئيسي وبياناتها
            //    .Include(ct => ct.CourseDetails)
            //        .ThenInclude(cd => cd.Locations) // إذا كان الموقع جدول منفصل
            //    .FirstOrDefaultAsync(ct => ct.Id == id);


            var courseTrainee = await _context.CourseTrainees
    .Include(ct => ct.Trainee)
    .Include(ct => ct.CourseDetails)
        .ThenInclude(cd => cd.Course) // جلب الدورة من تفاصيل الدورة
    .Include(ct => ct.CourseDetails)
        .ThenInclude(cd => cd.Locations) // جلب الموقع من تفاصيل الدورة
    .FirstOrDefaultAsync(ct => ct.Id == id);

            if (courseTrainee == null)
            {
                return NotFound("بيانات التسجيل غير موجودة.");
            }

            if ( courseTrainee.CourseDetails == null || courseTrainee.CourseDetails.Course == null)
            {
                // هذا يعني أن بعض البيانات المرتبطة مفقودة، يجب معالجة هذا
                return BadRequest("بيانات مرتبطة بالدورة أو المتدرب مفقودة.");
            }



            var viewModel = new CertificateViewModel
            {
                TraineeName = courseTrainee.Trainee.ArName, // افترض أن لديك خاصية FullName في Trainee
                CourseName = courseTrainee.CourseDetails.Course.Name,
                CourseStartDate = courseTrainee.CourseDetails.StartDate,
                CourseEndDate = courseTrainee.CourseDetails.EndDate,
                CourseDurationHours = courseTrainee.CourseDetails.DurationHours,
                CourseNumberoftargets = courseTrainee.CourseDetails.Numberoftargets,
                CourseLocation = courseTrainee.CourseDetails.Locations?.Name ?? "غير محدد", // افترض أن Location له Name
                CertificateNumber = courseTrainee.CertificateNumber,
                CertificateIssueDate = courseTrainee.CertificateIssueDate ?? DateTime.Now, // تاريخ اليوم كافتراضي إذا لم يحدد
            };

            return View("PrintCertificate", viewModel);
        }

        public async Task<IActionResult> CourseDetailsPdfView(Guid id) // id هو CourseDetailsId
        {
            var courseDetails = await _context.CourseDetails
                .Include(cd => cd.Course)
                    .ThenInclude(c => c.CourseClassification)
                .Include(cd => cd.Course)
                    .ThenInclude(c => c.Level)
                .Include(cd => cd.Locations)
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
                Name = courseDetails.Name,
                ClasifcationName = courseDetails.Course?.CourseClassification?.Name ?? "N/A",
                DurationHours = courseDetails.DurationHours,
                Numberoftargets = courseDetails.Numberoftargets,
                LocationName = courseDetails.Locations?.Name ?? "غير محدد",
                CourseTypeName =  "غير محدد",
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
                    ProfileImgeUrl = ct.Trainee?.ProfileImageUrl, // افترض أن لديك خاصية ProfileImageUrl في Trainee
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


        //===========  أكشن البحث الذي سيستخدمه Select2 (AJAX)  ===========//
        [HttpGet]
        public async Task<IActionResult> SearchTrainers(string term, Guid courseDetailsIdToExclude)
        {
            // الخطوة 1: الحصول على معرفات المدربين المسجلين بالفعل في هذه الدورة
            var assignedTrainerIds = await _context.CoursDetailsTrainer
                .Where(cdt => cdt.CourseDetailsId == courseDetailsIdToExclude)
                .Select(cdt => cdt.TrainerId)
                .ToListAsync();

            // الخطوة 2: البحث عن المدربين بالاسم مع استبعاد المسجلين منهم
            var query = _context.Trainers.AsQueryable();

            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(t => t.ArName.Contains(term)); // افترض أن اسم المدرب الكامل في FullName
            }

            // استبعاد المدربين المعينين مسبقاً
            query = query.Where(t => !assignedTrainerIds.Contains(t.Id));

            // الخطوة 3: تحويل النتائج إلى الصيغة التي يفهمها Select2
            var trainers = await query
                .Take(20) // لجلب عدد محدود من النتائج
                .Select(t => new { id = t.Id, text = t.ArName })
                .ToListAsync();

            return Json(new { results = trainers });
        }


        //===========  GET: CourseDetailsTrainers/Create  ===========//
        [HttpGet]
        public async Task<IActionResult> CreateCoursDetailsTrainer(Guid courseDetailsId)
        {
            var courseDetails = await _context.CourseDetails
                                              .Include(cd => cd.Course)
                                              .FirstOrDefaultAsync(cd => cd.Id == courseDetailsId);

            if (courseDetails == null)
            {
                return NotFound();
            }

            var viewModel = new AssignTrainerViewModel
            {
                CourseDetailsId = courseDetailsId,
                CourseDetailsName = courseDetails.Course.Name
            };

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCoursDetailsTrainer(AssignTrainerViewModel model)
        {
            if (ModelState.IsValid)
            {
                // ابحث عن المدربين المسجلين بالفعل لتجنب التكرار
                var isAlreadyAssigned = await _context.CoursDetailsTrainer
                    .AnyAsync(cdt => cdt.CourseDetailsId == model.CourseDetailsId && cdt.TrainerId == model.SelectedTrainerId);

                if (isAlreadyAssigned)
                {
                    ModelState.AddModelError("SelectedTrainerId", "هذا الشخص معين بالفعل في هذا البرنامج التدريبي.");
                    // يجب إعادة تهيئة المودل قبل إرساله مرة أخرى
                    return View(model);
                }

                var courseDetailsTrainer = new CoursDetailsTrainer
                {
                    CourseDetailsId = model.CourseDetailsId,
                    TrainerId = model.SelectedTrainerId,
                    //State = model.IsTrainer // <-- هنا نستخدم القيمة الجديدة
                };

                _context.Add(courseDetailsTrainer);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "تم تعيين الشخص بنجاح.";
                return RedirectToAction("Details", "CourseDetails", new { id = model.CourseDetailsId });
            }

            // إذا فشل الـ validation، ارجع إلى نفس الصفحة
            return View(model);
        }


    }






}
