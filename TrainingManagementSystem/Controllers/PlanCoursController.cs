using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.Models.Entities; // استبدل بمكان النماذج الخاصة بك
using TrainingManagementSystem.ViewModels; // استبدل بمكان الـ ViewModels الخاصة بك

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

        public async Task<IActionResult> Index(string viewMode = "Timeline", int? year = null, int? quarter = null)
        {
            // تحديد القيم الافتراضية
            viewMode = string.IsNullOrEmpty(viewMode) ? "Timeline" : viewMode;
            int currentYear = year ?? DateTime.Today.Year;
            int currentQuarter = quarter ?? (DateTime.Today.Month - 1) / 3 + 1;

            ViewData["ViewMode"] = viewMode;
            ViewData["SelectedYear"] = currentYear;
            ViewData["SelectedQuarter"] = currentQuarter;
            ViewData["Years"] = Enumerable.Range(DateTime.Today.Year - 5, 10)
                                        .Select(y => new SelectListItem { Value = y.ToString(), Text = y.ToString() });

            IQueryable<PlanCours> query = _context.PlanCours
                                                .Include(p => p.Course)
                                                .Include(p => p.Locations);

            switch (viewMode)
            {
                case "Location":
                    // جلب كل الدورات للسنة المختارة وتجميعها حسب الموقع
                    var plansForYear = await query
                        .Where(p => p.StartDate.Year == currentYear)
                        .ToListAsync();

                    // تجميع البيانات في Controller لتبسيط الـ View
                    var groupedByLocation = plansForYear
                        .Where(p => p.Locations != null)
                        .GroupBy(p => p.Locations)
                        .ToDictionary(g => g.Key, g => g.OrderBy(p => p.StartDate).ToList());

                    return View("Index", groupedByLocation); // نمرر القاموس كـ Model

                case "List":
                    // عرض قائمة بسيطة لكل الدورات في السنة
                    var listPlans = await query
                        .Where(p => p.StartDate.Year == currentYear)
                        .OrderBy(p => p.StartDate).ToListAsync();
                    return View("Index", listPlans);

                case "Calendar":
                    // وضع التقويم لا يحتاج إلى تمرير بيانات هنا، سيتم جلبها عبر AJAX
                    return View("Index", new List<PlanCours>());

                case "Timeline":
                default:
                    // نفس المنطق السابق للخطة الزمنية الربعية

                    var timelinePlans = await query
                        .OrderBy(p => p.StartDate)
                        .ToListAsync();
                    return View("Index", timelinePlans);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCalendarEvents(DateTime start, DateTime end)
        {
            var events = await _context.PlanCours
                 .Where(p => p.StartDate < end && (p.EndDate ?? p.StartDate) >= start)
                .Select(p => new
                {
                    // الخصائص التي يفهمها FullCalendar JS
                    id = p.Id,
                    title = p.Course.Name,
                    start = p.StartDate.ToString("yyyy-MM-dd"),
                    end = p.EndDate.Value.AddDays(1).ToString("yyyy-MM-dd"), // FullCalendar يتعامل مع النهاية بشكل حصري
                    color = GetRandomColor(p.CourseId), // دالة مساعدة لتلوين الحدث
                    url = Url.Action("Details", "PlanCours", new { id = p.Id }) // رابط عند النقر على الحدث
                })
                .ToListAsync();

            return Json(events);
        }

        private static string GetRandomColor(Guid? id)
        {
            if (!id.HasValue) return "#6c757d"; // Dark Gray
            var hash = id.Value.GetHashCode();
            // استخدام عمليات bitwise لتوليد ألوان أكثر تبايناً
            var r = (hash & 0xFF0000) >> 16;
            var g = (hash & 0x00FF00) >> 8;
            var b = hash & 0x0000FF;
            // التأكد من أن الألوان ليست داكنة جداً (لأن النص أبيض)
            return $"rgb({(r % 180) + 75}, {(g % 180) + 75}, {(b % 180) + 75})";
        }
     
        public async Task<IActionResult> Create()
        {
            var viewModel = new CourseFormViewModel();
            // استدعاء الدالة المساعدة لملء كل القوائم المنسدلة والبيانات اللازمة للعرض
            await PopulateViewModelDataAsync(viewModel);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CourseFormViewModel viewModel)
        {
           
                foreach (var detailEntry in viewModel.CourseDetailsEntries)
                {
                var planCours = new PlanCours
                {
                    CourseId = viewModel.Id,
                    Created = DateTime.Now,
                    DurationHours = detailEntry.DurationHours,
                    StartDate = detailEntry.StartDate,
                    EndDate = detailEntry.EndDate,
                    Numberoftargets = detailEntry.Numberoftargets,
                    LocationId = detailEntry.LocationId,
                    // أكمل ربط بقية الخصائص مثل CourseTypeId و StatusId إذا كانت موجودة
                };
                    _context.Add(planCours);
                }

                // حفظ كل التغييرات إلى قاعدة البيانات في عملية واحدة
                await _context.SaveChangesAsync();

                // توجيه المستخدم إلى صفحة النجاح (عادةً صفحة العرض الرئيسية)
                return RedirectToAction("Index");
        
        }


        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var planCours = await _context.PlanCours
                .Include(p => p.Course)
                .Include(p => p.Locations)
                .Include(p => p.PlanCoursTrainers)
                    .ThenInclude(pt => pt.Trainer) // لجلب بيانات المدرب نفسه
                .FirstOrDefaultAsync(m => m.Id == id);

            if (planCours == null)
            {
                return NotFound();
            }

            var existingTrainerIds = planCours.PlanCoursTrainers.Select(pt => pt.TrainerId);
            var availableTrainers = await _context.Trainers
                                        .Where(t => !existingTrainerIds.Contains(t.Id))
                                        .ToListAsync();

            ViewBag.TrainersList = new SelectList(availableTrainers, "Id", "ArName");

            return View(planCours);
        }

        [HttpGet]
        public async Task<IActionResult> GetPlanCourseEntryPartial(int index)
        {
            ViewData["index"] = index;

            ViewData["LocationsWithData"] = await _context.Locations
                .OrderBy(l => l.Name)
                .Select(l => new { Id = l.Id.ToString(), l.Name, l.FullCount })
                .ToListAsync();


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
        [HttpGet]
        public async Task<IActionResult> GetCourseDetailEntryPartial(int index)
        {
            // نحتاج لتمرير القوائم المنسدلة إلى البارشل فيو
            var parentViewModelForDropdowns = new CourseFormViewModel();
            await PopulateDropdownsAsync(parentViewModelForDropdowns); // استخدام الدالة المساعدة

            ViewData["index"] = index;
            ViewData["ParentViewModel"] = parentViewModelForDropdowns;

            // تمرير قائمة المواقع مع بيانات FullCount
            ViewData["LocationsWithData"] = await _context.Locations
                .OrderBy(l => l.Name)
                .Select(l => new { Id = l.Id.ToString(), l.Name, l.FullCount })
                .ToListAsync();

            return PartialView("_CourseDetailFormEntry", new CourseDetailFormEntryViewModel { StartDate = DateTime.Today });
        }

        private async Task PopulateDropdownsAsync(CourseFormViewModel viewModel)
        {
            // 1. قائمة الدورات للاختيار كقاعدة (للكروت)
            viewModel.CourseList = await _context.Courses
                .Include(c => c.Level)
                .Include(c => c.CourseClassification)
                .OrderBy(c => c.Name)
                .ToListAsync();

            // 2. القوائم المنسدلة الرئيسية (إذا كانت في الفورم الرئيسي)
            viewModel.CourseClassifications = await _context.CourseClassifications
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();

            viewModel.Levels = await _context.Levels
                .OrderBy(l => l.Name)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name })
                .ToListAsync();

            // 3. القوائم المنسدلة للبارشل فيو
            viewModel.Locations = await _context.   Locations
                .OrderBy(l => l.Name)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name })
                .ToListAsync();

            viewModel.CourseTypes = await _context.CourseTypes
                .OrderBy(ct => ct.Name)
                .Select(ct => new SelectListItem { Value = ct.Id.ToString(), Text = ct.Name })
                .ToListAsync();

            viewModel.Statuses = await _context.Statuses
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                .ToListAsync();

            // إضافة خيار "-- اختر --"
            viewModel.CourseClassifications.Insert(0, new SelectListItem { Value = "", Text = "-- اختر المحور --" });
            viewModel.Levels.Insert(0, new SelectListItem { Value = "", Text = "-- اختر المستوى --" });
            viewModel.Locations.Insert(0, new SelectListItem { Value = "", Text = "-- اختر الموقع --" });
            viewModel.CourseTypes.Insert(0, new SelectListItem { Value = "", Text = "-- اختر نوع الدورة --" });
            viewModel.Statuses.Insert(0, new SelectListItem { Value = "", Text = "-- اختر الحالة --" });

            // تمرير البيانات الإضافية للمواقع عبر ViewData
            ViewData["LocationsWithData"] = await _context.Locations
                .OrderBy(l => l.Name)
                .Select(l => new { Id = l.Id.ToString(), l.Name, l.FullCount })
                .ToListAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTrainerToPlan(Guid planCoursId, Guid trainerId)
        {
         _context.PlanCoursTrainers.Add(new PlanCoursTrainer
            {
                PlanCoursId = planCoursId,
                TrainerId = trainerId,
                Created = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = planCoursId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveTrainerFromPlan(Guid planCoursId, Guid trainerId)
        {
          _context.PlanCoursTrainers.RemoveRange(
                _context.PlanCoursTrainers.Where(pt => pt.PlanCoursId == planCoursId && pt.TrainerId == trainerId)
            );
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = planCoursId });
        }

        // GET: PlanCourses/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var planCours = await _context.PlanCours.FindAsync(id);
            if (planCours == null)
            {
                return NotFound();
            }

            // تزويد الـ View بقائمة الدورات لتعبئة القائمة المنسدلة
            ViewData["CoursesList"] = new SelectList(_context.Courses, "Id", "Name", planCours.CourseId);

            // تزويد الـ View بقائمة المواقع لتعبئة القائمة المنسدلة
            ViewData["LocationsList"] = new SelectList(_context.Locations, "Id", "Name", planCours.LocationId);

            return View(planCours);
        }


        // POST: PlanCourses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,CourseId,DurationHours,StartDate,EndDate,Numberoftargets,LocationId")] PlanCours planCours)
        {
            if (id != planCours.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(planCours);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // ... التعامل مع الخطأ ...
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            // في حالة فشل التحقق، أعد تعبئة القوائم قبل عرض النموذج مرة أخرى
            ViewData["CoursesList"] = new SelectList(_context.Courses, "Id", "Name", planCours.CourseId);
            ViewData["LocationsList"] = new SelectList(_context.Locations, "Id", "Name", planCours.LocationId);

            return View(planCours);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Delete(Guid id)
        {
            var planCours = await _context.PlanCours.FindAsync(id);
            if (planCours == null)
            {
                return NotFound();
            }
            _context.PlanCours.Remove(planCours);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> PrintPlanAsPdf(Guid id)
        {
            //var planCours = await _context.PlanCours
            //    .Include(p => p.Course)
            //    .Include(p => p.Locations)
            //    .Include(p => p.PlanCoursTrainers)
            //        .ThenInclude(pt => pt.Trainer) // لجلب بيانات المدرب نفسه
            //    .AsNoTracking() // مهم لتحسين الأداء في التقارير للقراءة فقط
            //    .FirstOrDefaultAsync(p => p.Id == id);

            //if (planCours == null)
            //{
            //    return NotFound();
            //}

            //// إرجاع View مخصص للطباعة كـ PDF
            return NotFound();
            //{

            //};
        }
    }
}
