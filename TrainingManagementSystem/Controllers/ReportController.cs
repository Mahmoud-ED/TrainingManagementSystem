// Controllers/ReportController.cs
using Microsoft.AspNetCore.Authorization;
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

[ViewLayout("_LayoutDashboard")]
[Authorize(Roles = "Admin,Prog")]
public class ReportController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Chart()
    {
        return View();
    }
    [HttpGet]
    public async Task<JsonResult> GetKpiData()
    {
        var currentYear = DateTime.Now.Year;

        var totalCourses = await _context.CourseDetails
            .Where(cd => cd.StartDate.Year == currentYear)
            .CountAsync();

        var totalTrainees = await _context.Trainees
            .CountAsync();

        var averageAttendance = await _context.CourseTrainees
            .Where(ct => ct.AttendancePercentage.HasValue && ct.CourseDetails.StartDate.Year == currentYear)
            .AverageAsync(ct => (decimal?)ct.AttendancePercentage) ?? 0;

        var activeOrganizations = await _context.Trainees
            .Where(t => t.OrganizationId.HasValue && t.CourseTrainees.Any(ct => ct.CourseDetails.StartDate.Year == currentYear))
            .Select(t => t.OrganizationId)
            .Distinct()
            .CountAsync();

        return Json(new
        {
            totalCourses,
            totalTrainees,
            averageAttendance = Math.Round(averageAttendance, 2),
            activeOrganizations
        });
    }

    // Action لجلب بيانات أبرز الجهات
    [HttpGet]
    public async Task<JsonResult> GetTopOrganizationsChart()
    {
        var data = await _context.Trainees
            .Where(t => t.OrganizationId != null && t.Organizition != null)
            .GroupBy(t => t.Organizition.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        return Json(new { series = data.Select(d => d.Count), labels = data.Select(d => d.Name) });
    }

    // Action لجلب بيانات أداء المدربين
    [HttpGet]
    public async Task<JsonResult> GetTopTrainersChart()
    {
        // هذا الاستعلام معقد قليلاً لأنه يمر عبر عدة جداول
        var data = await _context.Trainers
            .Where(t => t.CoursDetailsTrainer.Any(cdt => cdt.CourseDetails.CourseTrainees.Any(ct => ct.Grade.HasValue))) // مدربون لديهم دورات بها تقييمات
            .Select(t => new {
                TrainerName = t.ArName,
                // حساب متوسط التقييمات لكل الدورات التي دربها
                AverageGrade = t.CoursDetailsTrainer
                                .SelectMany(cdt => cdt.CourseDetails.CourseTrainees)
                                .Where(ct => ct.Grade.HasValue)
                                .Average(ct => (decimal?)ct.Grade) ?? 0
            })
            .Where(t => t.AverageGrade > 0)
            .OrderByDescending(t => t.AverageGrade)
            .Take(10)
            .ToListAsync();

        return Json(new { series = data.Select(d => Math.Round(d.AverageGrade, 2)), labels = data.Select(d => d.TrainerName) });
    }

    // Action لجلب بيانات تطور عدد المتدربين
    [HttpGet]
    public async Task<JsonResult> GetTraineeEnrollmentChart()
    {
        var data = await _context.CourseTrainees
            .Where(ct => ct.CourseDetails != null)
            .GroupBy(ct => new { ct.CourseDetails.StartDate.Year, ct.CourseDetails.StartDate.Month })
            .Select(g => new {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .Take(12) // آخر 12 شهر من النشاط
            .ToListAsync();

        // تحويل البيانات لشكل مناسب للمخطط
        var labels = data.Select(d => new DateTime(d.Year, d.Month, 1).ToString("MMM yyyy")).ToList();
        var series = data.Select(d => d.Count).ToList();

        return Json(new { series, labels });
    }


    // أضف هذا الـ Action الجديد إلى DashboardController.cs

    [HttpGet]
    public async Task<JsonResult> GetPlanVsActualAnalysis()
    {
        // 1. جلب كل الدورات المخطط لها لهذا العام (كمثال)
        var currentYear = DateTime.Now.Year;
        var plannedCourses = await _context.CourseDetails
            .Include(p => p.Course) // نحتاج اسم الدورة من الجدول الرئيسي
            .Where(p => p.StartDate.Year == currentYear)
            .ToListAsync();

        // 2. جلب كل الدورات المنفذة فعلياً التي لها أصل في الخطة
        var executedCourseIds = await _context.CourseDetails
            .Where(cd => cd.StartDate.Year == currentYear)
            .Select(cd => cd.CourseId) // نفترض أن الربط يتم عبر CourseId
            .Distinct()
            .ToListAsync();

        // 3. تحليل البيانات
        var totalPlanned = plannedCourses.Count;
        if (totalPlanned == 0)
        {
            // لا يوجد شيء للمقارنة
            return Json(new { percentage = 0, executedCount = 0, upcomingCount = 0, notExecutedCount = 0, details = new List<object>() });
        }

        var executedCount = plannedCourses.Count(p => executedCourseIds.Contains(p.CourseId));
        var upcomingCount = plannedCourses.Count(p => p.StartDate > DateTime.Now && !executedCourseIds.Contains(p.CourseId));



        var notExecutedCount = plannedCourses.Count(p => p.StartDate <= DateTime.Now && !executedCourseIds.Contains(p.CourseId));

        // حساب النسبة المئوية للإنجاز
        var percentage = (double)executedCount / totalPlanned * 100;

        // 4. تجهيز قائمة تفصيلية
        var details = plannedCourses.Select(p => {
            string status;
            string statusColor;
            if (executedCourseIds.Contains(p.CourseId))
            {
                status = "تم التنفيذ";
                statusColor = "success"; // لون أخضر
            }
            else if (p.StartDate > DateTime.Now)
            {
                status = "قادمة";
                statusColor = "primary"; // لون أزرق
            }
            else
            {
                status = "لم تنفذ (متأخرة)";
                statusColor = "danger"; // لون أحمر
            }
            return new
            {
                CourseName = p.Course?.Name ?? "اسم غير محدد",
                PlannedDate = p.StartDate.ToString("dd MMM yyyy"),
                Status = status,
                StatusColor = statusColor
            };
        }).OrderBy(d => d.PlannedDate).ToList();


        return Json(new
        {
            percentage = Math.Round(percentage, 1),
            executedCount,
            upcomingCount,
            notExecutedCount,
            details
        });
    }

}
