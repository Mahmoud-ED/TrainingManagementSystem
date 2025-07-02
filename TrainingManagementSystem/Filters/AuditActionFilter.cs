using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using TrainingManagementSystem.Models;
using TrainingManagementSystem.Models.Entities;

namespace TrainingManagementSystem.Filters
{
    // === الكلاس الثاني: المنطق الفعلي للفلتر ===
    public class AuditActionFilter : IActionFilter
    {
        private readonly ApplicationDbContext _context;
        private readonly string _actionType;
        private readonly string _entityName;

        public AuditActionFilter(ApplicationDbContext context, string actionType, string entityName)
        {
            _context = context;
            _actionType = actionType;
            _entityName = entityName;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception == null && context.HttpContext.User.Identity.IsAuthenticated)
            {
                var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = context.HttpContext.User.FindFirstValue(ClaimTypes.Name) ?? "N/A";

                var log = new AuditLog
                {
                    UserId = userId,
                    ActionType = _actionType,
                    EntityName = _entityName,
                    Timestamp = DateTime.UtcNow,
                    Details = $"المستخدم '{userName}' قام بعملية '{_actionType}' في شاشة '{_entityName}'."
                };

                _context.AuditLog.Add(log);
                _context.SaveChanges();
            }
        }
    }
}