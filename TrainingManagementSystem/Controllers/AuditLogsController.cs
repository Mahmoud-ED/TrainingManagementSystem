using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainingManagementSystem.Classes;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.Models.Entities;

[ViewLayout("_LayoutDashboard")]
[Authorize(Roles = "Admin,Prog")]
public class AuditLogsController : Controller
{

    private readonly ApplicationDbContext _context;

    public AuditLogsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(
        string userFilter,
        string actionFilter,
        string entityFilter,
        string startDate,
        string endDate)
    {
        // ابدأ بالاستعلام الأساسي مع تضمين بيانات المستخدم لتجنب مشكلة N+1
        var query = _context.AuditLog.Include(a => a.User).AsQueryable();

        // 1. فلترة حسب اسم المستخدم
        if (!string.IsNullOrEmpty(userFilter))
        {
            query = query.Where(a => a.User.UserName.Contains(userFilter));
        }

        // 2. فلترة حسب نوع العملية
        if (!string.IsNullOrEmpty(actionFilter))
        {
            query = query.Where(a => a.ActionType == actionFilter);
        }

        // 3. فلترة حسب الكيان
        if (!string.IsNullOrEmpty(entityFilter))
        {
            query = query.Where(a => a.EntityName.Contains(entityFilter));
        }

        // 4. فلترة حسب تاريخ البدء
        if (DateTime.TryParse(startDate, out DateTime start))
        {
            query = query.Where(a => a.Timestamp >= start);
        }

        // 5. فلترة حسب تاريخ الانتهاء
        if (DateTime.TryParse(endDate, out DateTime end))
        {
            // نضيف يومًا واحدًا لتضمين كامل اليوم الأخير في البحث
            end = end.AddDays(1);
            query = query.Where(a => a.Timestamp < end);
        }

        // ترتيب النتائج من الأحدث إلى الأقدم
        var auditLogs = await query.OrderByDescending(a => a.Timestamp).ToListAsync();

        return View(auditLogs);
    }
}
