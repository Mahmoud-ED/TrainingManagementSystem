using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Filters;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.ViewModels;

namespace TrainingManagementSystem.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    [Authorize(Roles = "Admin,Prog")]
    public class PlanCoursController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PlanCoursController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Audit("عرض", "خطة تدريبية")]
        public async Task<IActionResult> Index(string viewMode = "Gantt", int? year = null, int? month = null)
        {
            // 1. تحديد القيم الافتراضية
            viewMode = string.IsNullOrEmpty(viewMode) ? "Gantt" : viewMode;
            int currentYear = year ?? DateTime.Today.Year;
            int currentMonth = month ?? DateTime.Today.Month;

            // 2. تمرير البيانات الأساسية للـ View عبر ViewData
            ViewData["ViewMode"] = viewMode;
            ViewData["SelectedYear"] = currentYear;
            ViewData["SelectedMonth"] = currentMonth;
            ViewData["Years"] = Enumerable.Range(DateTime.Today.Year - 5, 10)
                                        .Select(y => new SelectListItem { Value = y.ToString(), Text = y.ToString(), Selected = (y == currentYear) });

            // 3. بناء الاستعلام الأساسي الذي سيعاد استخدامه
            var baseQuery = _context.PlanCours
                .Include(p => p.Course)
                .Include(p => p.Locations);

            // 4. التبديل بين طرق العرض المختلفة
            switch (viewMode)
            {
                case "Location":
                    var plansByLocation = await baseQuery
                        .Where(p => p.StartDate.Year == currentYear || (p.EndDate.HasValue && p.EndDate.Value.Year == currentYear))
                        .ToListAsync();
                    var groupedByLocation = plansByLocation
                        .Where(p => p.Locations != null)
                        .GroupBy(p => p.Locations)
                        .ToDictionary(g => g.Key, g => g.OrderBy(p => p.StartDate).ToList());
                    return View("Index", groupedByLocation);

                case "List":
                    var listPlans = await baseQuery
                        .Where(p => p.StartDate.Year == currentYear || (p.EndDate.HasValue && p.EndDate.Value.Year == currentYear))
                        .OrderBy(p => p.StartDate).ToListAsync();
                    return View("Index", listPlans);

                case "Calendar":
                    // التقويم لا يحتاج لبيانات أولية، سيتم جلبها عبر AJAX
                    return View("Index", new List<PlanCours>());

                case "Matrix":
                    var monthStartDate = new DateTime(currentYear, currentMonth, 1);
                    var monthEndDate = monthStartDate.AddMonths(1).AddDays(-1);

                    var plansInMonth = await baseQuery
                        .Where(p => p.StartDate <= monthEndDate && (p.EndDate ?? p.StartDate) >= monthStartDate)
                        .ToListAsync();

                    // استخدام الثقافة العربية المصرية لأسماء الشهور
                    var culture = new System.Globalization.CultureInfo("ar-EG");
                    var viewModel = new MatrixViewModel
                    {
                        Year = currentYear,
                        Month = currentMonth,
                        MonthName = culture.DateTimeFormat.GetMonthName(currentMonth),
                        NumberOfWeeks = 5 // 5 أسابيع لتغطية كل أيام الشهر
                    };

                    // تهيئة قوائم الإجماليات بالصفر
                    viewModel.WeeklyTotals = Enumerable.Repeat(0.0, viewModel.NumberOfWeeks).ToList();

                    foreach (var plan in plansInMonth)
                    {
                        var courseItem = new MatrixCourseItem { Plan = plan };
                        courseItem.WeeklyHours = Enumerable.Repeat(0.0, viewModel.NumberOfWeeks).ToList();

                        var totalDays = ((plan.EndDate ?? plan.StartDate) - plan.StartDate).TotalDays + 1;
                        double hoursPerDay = (totalDays > 0 && plan.DurationHours > 0) ? (double)plan.DurationHours / totalDays : 0;

                        // توزيع ساعات الدورة على أسابيع الشهر
                        for (var day = plan.StartDate; day <= (plan.EndDate ?? plan.StartDate); day = day.AddDays(1))
                        {
                            if (day.Year == currentYear && day.Month == currentMonth)
                            {
                                // حساب الأسبوع الحالي (0-4) بناءً على اليوم في الشهر
                                int weekOfMonth = (day.Day - 1) / 7;
                                if (weekOfMonth < viewModel.NumberOfWeeks)
                                {
                                    courseItem.WeeklyHours[weekOfMonth] += hoursPerDay;
                                }
                            }
                        }

                        viewModel.Courses.Add(courseItem);

                        // تحديث الإجمالي العام لكل أسبوع
                        for (int i = 0; i < viewModel.NumberOfWeeks; i++)
                        {
                            viewModel.WeeklyTotals[i] += courseItem.WeeklyHours[i];
                        }
                    }

                    viewModel.GrandTotal = viewModel.WeeklyTotals.Sum();
                    return View("Index", viewModel);

                case "Gantt":
                default:
                    var ganttPlans = await baseQuery
                        .Where(p => p.StartDate.Year == currentYear || (p.EndDate.HasValue && p.EndDate.Value.Year == currentYear))
                        .OrderBy(p => p.StartDate).ToListAsync();
                    return View("Index", ganttPlans);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetCalendarEvents(DateTime start, DateTime end)
        {
            var events = await _context.PlanCours
                 .Where(p => p.StartDate < end && (p.EndDate ?? p.StartDate) >= start)
                .Select(p => new
                {
                    id = p.Id,
                    title = p.Course.Name,
                    start = p.StartDate.ToString("yyyy-MM-dd"),
                    end = p.EndDate.HasValue ? p.EndDate.Value.AddDays(1).ToString("yyyy-MM-dd") : null,
                    color = GetEventColor(p.CourseId),
                    url = Url.Action("Details", "PlanCours", new { id = p.Id })
                })
                .ToListAsync();

            return Json(events);
        }

        private static string GetEventColor(Guid? id)
        {
            if (!id.HasValue) return "#6c757d";
            var hash = id.Value.GetHashCode();
            var r = (hash & 0xFF0000) >> 16;
            var g = (hash & 0x00FF00) >> 8;
            var b = hash & 0x0000FF;
            return $"rgb({(r % 180) + 75}, {(g % 180) + 75}, {(b % 180) + 75})";
        }

        [Audit("انشاء-عرض", "خطة تدريبية")]
        public async Task<IActionResult> Create()
        {



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
            await PopulateViewModelDataAsync(viewModel);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Audit("إنشاء", "خطة تدريبية")]
        public async Task<IActionResult> Create(CourseFormViewModel viewModel)
        {
            if (viewModel.CourseDetailsEntries == null || !viewModel.CourseDetailsEntries.Any())
            {
                ModelState.AddModelError("", "يجب إضافة تفصيل تنفيذ واحد على الأقل للخطة.");
                await PopulateViewModelDataAsync(viewModel);
                return View(viewModel);
            }

            //if (ModelState.IsValid)
            //{
                foreach (var detailEntry in viewModel.CourseDetailsEntries)
                {
                    var planCours = new PlanCours
                    {
                        Id = Guid.NewGuid(),
                        CourseId = viewModel.Id,
                        Created = DateTime.Now,
                        DurationHours = detailEntry.DurationHours,
                        StartDate = detailEntry.StartDate,
                        EndDate = detailEntry.EndDate,
                        Numberoftargets = detailEntry.Numberoftargets,
                        LocationId = detailEntry.LocationId,
                    };
                    _context.Add(planCours);
                }
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "تم إنشاء الخطة بنجاح!";
                return RedirectToAction("Index");
            //}

            //await PopulateViewModelDataAsync(viewModel);
            //return View(viewModel);
        }

        [Audit("تفاصيل", "خطة تدريبية")]
        public async Task<IActionResult> Details(Guid id)
        {
            var planCours = await _context.PlanCours
                .Include(p => p.Course)
                .Include(p => p.Locations)
                .Include(p => p.PlanCoursTrainers).ThenInclude(pt => pt.Trainer)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (planCours == null) return NotFound();

            var existingTrainerIds = planCours.PlanCoursTrainers.Select(pt => pt.TrainerId);
            ViewBag.TrainersList = new SelectList(
                await _context.Trainers.Where(t => !existingTrainerIds.Contains(t.Id)).ToListAsync(),
                "Id", "ArName");

            return View(planCours);
        }

        //[HttpGet]
        //public async Task<IActionResult> GetCourseDetailEntryPartial(int index)
        //{
        //    ViewData["index"] = index;
        //    ViewData["LocationsWithData"] = await _context.Locations
        //        .OrderBy(l => l.Name)
        //        .Select(l => new { Id = l.Id.ToString(), l.Name, l.FullCount })
        //        .ToListAsync();
        //    return PartialView("_CourseDetailFormEntry", new CourseDetailFormEntryViewModel { StartDate = DateTime.Today });
        //}


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
                // القوائم الأخرى تبقى كما هي
                CourseTypes = (await _context.CourseTypes
                    .OrderBy(ct => ct.Name)
                    .Select(ct => new SelectListItem { Value = ct.Id.ToString(), Text = ct.Name })
                    .ToListAsync()),

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

            return PartialView("_PlanCourseEntryPartial", new CourseDetailFormEntryViewModel { StartDate = DateTime.Today });
        }

        private async Task PopulateViewModelDataAsync(CourseFormViewModel viewModel)
        {
            viewModel.CourseList = await _context.Courses
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewData["LocationsWithData"] = await _context.Locations
                .OrderBy(l => l.Name)
                .Select(l => new { Id = l.Id.ToString(), l.Name, l.FullCount })
                .ToListAsync();
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            var planCours = await _context.PlanCours.FindAsync(id);
            if (planCours == null) return NotFound();

            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Name", planCours.CourseId);
            ViewData["LocationId"] = new SelectList(_context.Locations, "Id", "Name", planCours.LocationId);
            return View(planCours);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Audit("تعديل", "خطة تدريبية")]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,CourseId,DurationHours,StartDate,EndDate,Numberoftargets,LocationId,Created")] PlanCours planCours)
        {
            if (id != planCours.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(planCours);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.PlanCours.Any(e => e.Id == planCours.Id)) return NotFound();
                    else throw;
                }
                TempData["SuccessMessage"] = "تم تعديل بيانات الدورة بنجاح.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Name", planCours.CourseId);
            ViewData["LocationId"] = new SelectList(_context.Locations, "Id", "Name", planCours.LocationId);
            return View(planCours);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Audit("حذف", "خطة تدريبية")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var planCours = await _context.PlanCours.FindAsync(id);
            if (planCours == null) return NotFound();
            _context.PlanCours.Remove(planCours);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "تم حذف الدورة من الخطة بنجاح.";
            return Json(new { success = true, message = "تم الحذف بنجاح" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTrainerToPlan(Guid planCoursId, Guid trainerId)
        {
            if (await _context.PlanCoursTrainers.AnyAsync(pt => pt.PlanCoursId == planCoursId && pt.TrainerId == trainerId))
            {
                TempData["ErrorMessage"] = "المدرب مضاف مسبقاً لهذه الدورة.";
                return RedirectToAction(nameof(Details), new { id = planCoursId });
            }

            _context.PlanCoursTrainers.Add(new PlanCoursTrainer { PlanCoursId = planCoursId, TrainerId = trainerId, Created = DateTime.Now });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "تمت إضافة المدرب بنجاح.";
            return RedirectToAction(nameof(Details), new { id = planCoursId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveTrainerFromPlan(Guid planCoursId, Guid trainerId)
        {
            var trainerPlan = await _context.PlanCoursTrainers.FirstOrDefaultAsync(pt => pt.PlanCoursId == planCoursId && pt.TrainerId == trainerId);
            if (trainerPlan != null)
            {
                _context.PlanCoursTrainers.Remove(trainerPlan);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "تمت إزالة المدرب بنجاح.";
            }
            return RedirectToAction(nameof(Details), new { id = planCoursId });
        }


        [HttpGet]
        [Audit("طباعة", "خطة تدريبية")]
        public async Task<IActionResult> PrintMatrix(int year, int month)
        {
            // --- هذا الكود هو نسخة طبق الأصل من كود تجهيز البيانات في دالة Index ---
            var monthStartDate = new DateTime(year, month, 1);
            var monthEndDate = monthStartDate.AddMonths(1).AddDays(-1);

            var plansInMonth = await _context.PlanCours
                .Include(p => p.Course)
                .Where(p => p.StartDate <= monthEndDate && (p.EndDate ?? p.StartDate) >= monthStartDate)
                .ToListAsync();

            var culture = new System.Globalization.CultureInfo("ar-EG");
            var viewModel = new MatrixViewModel
            {
                Year = year,
                Month = month,
                MonthName = culture.DateTimeFormat.GetMonthName(month),
                NumberOfWeeks = 5
            };

            viewModel.WeeklyTotals = Enumerable.Repeat(0.0, viewModel.NumberOfWeeks).ToList();

            foreach (var plan in plansInMonth)
            {
                var courseItem = new MatrixCourseItem { Plan = plan };
                courseItem.WeeklyHours = Enumerable.Repeat(0.0, viewModel.NumberOfWeeks).ToList();

                var totalDays = ((plan.EndDate ?? plan.StartDate) - plan.StartDate).TotalDays + 1;
                double hoursPerDay = (totalDays > 0 && plan.DurationHours > 0) ? (double)plan.DurationHours / totalDays : 0;

                for (var day = plan.StartDate; day <= (plan.EndDate ?? plan.StartDate); day = day.AddDays(1))
                {
                    if (day.Year == year && day.Month == month)
                    {
                        int weekOfMonth = (day.Day - 1) / 7;
                        if (weekOfMonth < viewModel.NumberOfWeeks)
                        {
                            courseItem.WeeklyHours[weekOfMonth] += hoursPerDay;
                        }
                    }
                }

                viewModel.Courses.Add(courseItem);

                for (int i = 0; i < viewModel.NumberOfWeeks; i++)
                {
                    viewModel.WeeklyTotals[i] += courseItem.WeeklyHours[i];
                }
            }

            viewModel.GrandTotal = viewModel.WeeklyTotals.Sum();

            // --- هنا الاختلاف: نعيد View جديد مخصص للطباعة ---
            return View("PrintMatrix", viewModel);
        }




        // GET: /PlanCours/AdvancedCopy
        // هذه الدالة تعرض الصفحة وتجلب البيانات إذا تم اختيار سنة مصدر
        [HttpGet]
        public async Task<IActionResult> AdvancedCopy(int? sourceYear)
        {
            var viewModel = new AdvancedCopyViewModel
            {
                SourceYear = sourceYear,
                YearList = Enumerable.Range(DateTime.Today.Year - 5, 10)
                                     .Select(y => new SelectListItem
                                     {
                                         Value = y.ToString(),
                                         Text = y.ToString()
                                     }).Reverse()
            };

            if (sourceYear.HasValue)
            {
                // إذا اختار المستخدم سنة، اجلب كل الخطط من تلك السنة
                viewModel.PlansToCopy = await _context.PlanCours
                    .Where(p => p.StartDate.Year == sourceYear.Value)
                    .Include(p => p.Course)
                    .Include(p => p.Locations)
                    .Select(p => new PlanToCopyItem
                    {
                        PlanId = p.Id,
                        CourseName = p.Course.Name,
                        OriginalStartDate = p.StartDate,
                        LocationName = p.Locations.Name ?? "N/A",
                        IsSelected = true // جعل كل الخيارات محددة بشكل افتراضي للراحة
                    })
                    .OrderBy(p => p.OriginalStartDate)
                    .ToListAsync();
            }

            return View(viewModel);
        }


        // POST: /PlanCours/AdvancedCopy
        // هذه الدالة تنفذ عملية النسخ الفعلية بعد أن يختار المستخدم
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdvancedCopy(AdvancedCopyViewModel viewModel)
        {
            if (viewModel.SourceYear == viewModel.TargetYear)
            {
                TempData["ErrorMessage"] = "لا يمكن النسخ إلى نفس السنة المصدر.";
                return RedirectToAction(nameof(AdvancedCopy), new { sourceYear = viewModel.SourceYear });
            }

            if (viewModel.SelectedPlanIds == null || !viewModel.SelectedPlanIds.Any())
            {
                TempData["ErrorMessage"] = "يجب اختيار برنامج واحد على الأقل لنسخه.";
                return RedirectToAction(nameof(AdvancedCopy), new { sourceYear = viewModel.SourceYear });
            }

            // جلب الخطط الأصلية التي تم اختيارها فقط
            var plansToCopy = await _context.PlanCours
                .AsNoTracking()
                .Where(p => viewModel.SelectedPlanIds.Contains(p.Id))
                .ToListAsync();

            var newPlans = new List<PlanCours>();
            foreach (var oldPlan in plansToCopy)
            {
                int yearDifference = viewModel.TargetYear - oldPlan.StartDate.Year;

                var newPlan = new PlanCours
                {
                    Id = Guid.NewGuid(),
                    CourseId = oldPlan.CourseId,
                    DurationHours = oldPlan.DurationHours,
                    Numberoftargets = oldPlan.Numberoftargets,
                    LocationId = oldPlan.LocationId,
                    StartDate = oldPlan.StartDate.AddYears(yearDifference),
                    EndDate = oldPlan.EndDate?.AddYears(yearDifference),
                    Created = DateTime.Now,
                };
                newPlans.Add(newPlan);
            }

            await _context.PlanCours.AddRangeAsync(newPlans);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"تم نسخ {newPlans.Count} برنامج بنجاح إلى سنة {viewModel.TargetYear}.";
            return RedirectToAction(nameof(Index), new { year = viewModel.TargetYear, viewMode = "List" });
        }

    }
}