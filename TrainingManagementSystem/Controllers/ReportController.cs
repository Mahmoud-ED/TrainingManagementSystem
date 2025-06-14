// Controllers/ReportController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.Models.Entities; // استبدل هذا بمساحة اسم Entities الخاصة بك

// ViewModel للمخططات في صفحة التقرير
public class ReportChartViewModel
{
    // فلاتر
    [Display(Name = "من تاريخ")]
    [DataType(DataType.Date)]
    public DateTime? StartDateFilter { get; set; }

    [Display(Name = "إلى تاريخ")]
    [DataType(DataType.Date)]
    public DateTime? EndDateFilter { get; set; }

    [Display(Name = "تصنيف الدورة")]
    public Guid? CourseClassificationIdFilter { get; set; }
    public SelectList CourseClassifications { get; set; }

    [Display(Name = "حالة الدورة")]
    public Guid? StatusIdFilter { get; set; }
    public SelectList Statuses { get; set; }


    // بيانات المخططات (يمكنك إضافة المزيد حسب الحاجة)
    public ChartData TraineesPerCourseStatusData { get; set; } // عدد المتدربين حسب حالة الدورة
    public ChartData CoursesCountPerClassificationData { get; set; } // عدد الدورات حسب المحور
    public ChartData AverageGradePerCourseData { get; set; } // متوسط الدرجات لكل دورة (مثال)

    public ReportChartViewModel()
    {
        TraineesPerCourseStatusData = new ChartData();
        CoursesCountPerClassificationData = new ChartData();
        AverageGradePerCourseData = new ChartData();
    }
}

public class ChartData
{
    public List<string> Labels { get; set; } = new List<string>();
    public List<int> Data { get; set; } = new List<int>(); // أو List<decimal> للقيم العشرية
    public List<string> BackgroundColors { get; set; } = new List<string>(); // ألوان للمخططات الدائرية/الشريطية
    public string Label { get; set; } // عنوان مجموعة البيانات في المخطط
}

[ViewLayout("_LayoutDashboard")]
[Display(Name = "التقارير")] // ✅ Controller title

// Controllers/ReportController.cs
// ... (باقي using statements و class ReportChartViewModel و ChartData كما هي) ...

public class ReportController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Chart(DateTime? startDateFilter, DateTime? endDateFilter, Guid? courseClassificationIdFilter, Guid? statusIdFilter)
    {
        var viewModel = new ReportChartViewModel
        {
            StartDateFilter = startDateFilter,
            EndDateFilter = endDateFilter,
            CourseClassificationIdFilter = courseClassificationIdFilter,
            StatusIdFilter = statusIdFilter,
            CourseClassifications = new SelectList(await _context.CourseClassifications.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", courseClassificationIdFilter),
            Statuses = new SelectList(await _context.Statuses.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", statusIdFilter)
        };
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetChartData(DateTime? startDateFilter, DateTime? endDateFilter, Guid? courseClassificationIdFilter, Guid? statusIdFilter)
    {
        IQueryable<CourseDetails> query = _context.CourseDetails
                                                .AsNoTracking() // جيد للأداء
                                                .Include(cd => cd.Course)
                                                .Include(cd => cd.Status)
                                                .Include(cd => cd.Course.CourseClassification)
                                                .Include(cd => cd.CourseTrainees); // مهم لحساب المتدربين

        // ... (تطبيق الفلاتر كما هو) ...
        if (startDateFilter.HasValue)
        {
            query = query.Where(cd => cd.StartDate >= startDateFilter.Value);
        }
        if (endDateFilter.HasValue)
        {
            query = query.Where(cd => cd.StartDate <= endDateFilter.Value.AddDays(1).AddTicks(-1));
        }
        if (courseClassificationIdFilter.HasValue)
        {
            query = query.Where(cd => cd.Course.CourseClassificationId == courseClassificationIdFilter.Value);
        }
        if (statusIdFilter.HasValue)
        {
            query = query.Where(cd => cd.StatusId == statusIdFilter.Value);
        }


        var filteredCourseDetails = await query.ToListAsync();

        // 1. مخطط عدد المتدربين حسب حالة الدورة (كما هو)
        var traineesPerStatus = filteredCourseDetails
            .GroupBy(cd => cd.Status?.Name ?? "غير محددة")
            .Select(g => new { StatusName = g.Key, TraineeCount = g.Sum(cd => cd.CourseTrainees?.Count ?? 0) })
            .OrderBy(x => x.StatusName);
        var traineesPerCourseStatusData = new ChartData { Label = "عدد المتدربين", Labels = traineesPerStatus.Select(x => x.StatusName).ToList(), Data = traineesPerStatus.Select(x => x.TraineeCount).ToList() };

        // 2. مخطط عدد الدورات حسب المحور (كما هو)
        var coursesPerClassification = filteredCourseDetails
            .Where(cd => cd.Course?.CourseClassification != null)
            .GroupBy(cd => cd.Course.CourseClassification.Name)
            .Select(g => new { ClassificationName = g.Key, CourseCount = g.Count() })
            .OrderBy(x => x.ClassificationName);
        var coursesCountPerClassificationData = new ChartData { Label = "عدد الدورات", Labels = coursesPerClassification.Select(x => x.ClassificationName).ToList(), Data = coursesPerClassification.Select(x => x.CourseCount).ToList() };

        // 3. مخطط متوسط الدرجات لكل دورة مع عرض الفترة (كما هو)
        var averageGradePerCourse = filteredCourseDetails
            .Where(cd => cd.CourseTrainees != null && cd.CourseTrainees.Any(ct => ct.Grade.HasValue))
            .Select(cd => new { CourseName = cd.Course?.Name ?? "دورة غير محددة", StartDate = cd.StartDate, EndDate = cd.EndDate, AverageGrade = cd.CourseTrainees.Where(ct => ct.Grade.HasValue).Average(ct => ct.Grade.Value) })
            .OrderByDescending(x => x.AverageGrade).Take(10);
        var averageGradePerCourseData = new ChartData { Label = "متوسط الدرجات", Labels = averageGradePerCourse.Select(x => $"{x.CourseName} ({x.StartDate:dd/MM/yy} - {(x.EndDate.HasValue ? x.EndDate.Value.ToString("dd/MM/yy") : "مستمرة")})").ToList(), Data = averageGradePerCourse.Select(x => (int)Math.Round(x.AverageGrade)).ToList() };

        // 4. مخطط جديد: عدد المتدربين في كل تفصيل دورة (CourseDetails)
        var traineesPerCourseDetail = filteredCourseDetails
            .Select(cd => new
            {
                CourseDetailIdentifier = $"{cd.Course?.Name ?? "دورة غير محددة"} ({cd.StartDate:dd/MM/yy})", // معرّف فريد لكل تفصيل دورة
                TraineeCount = cd.CourseTrainees?.Count ?? 0
            })
            .Where(x => x.TraineeCount > 0) // عرض الدورات التي بها متدربون فقط (اختياري)
            .OrderByDescending(x => x.TraineeCount) // ترتيب تنازلي لعرض الأكثر تسجيلاً أولاً
            .Take(15); // خذ أفضل 15 مثلاً لتجنب ازدحام المخطط

        var traineesPerCourseDetailData = new ChartData
        {
            Label = "عدد المتدربين",
            Labels = traineesPerCourseDetail.Select(x => x.CourseDetailIdentifier).ToList(),
            Data = traineesPerCourseDetail.Select(x => x.TraineeCount).ToList()
        };

        return Json(new
        {
            traineesPerCourseStatusData,
            coursesCountPerClassificationData,
            averageGradePerCourseData,
            traineesPerCourseDetailData // إضافة البيانات الجديدة هنا
        });
    }
}
