using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingManagementSystem.Models.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string UserId { get; set; } // أو اسم المستخدم مباشرة
        public string ActionType { get; set; } // مثلاً: "إنشاء", "تعديل", "حذف"
        public string EntityName { get; set; } // اسم الشاشة أو الجدول, مثلاً: "PlanCours"
        public DateTime Timestamp { get; set; }
        public string Details { get; set; } // تفاصيل إضافية مثل "تم تعديل الخطة رقم 123"
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
}