using System.ComponentModel.DataAnnotations;
namespace TrainingManagementSystem.Models.Entities
{
    public class Attendance : BaseEntity
    {
        [Required]
        public Guid CourseDetailsId { get; set; }
        public CourseDetails CourseDetails { get; set; }

        [Required]
        public Guid TraineeId { get; set; }
        public Trainee Trainee { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime AttendanceDate { get; set; }

        [Required]
        public AttendanceStatus Status { get; set; } // ????/????/?????/????

        [MaxLength(250)]
        public string? Notes { get; set; }
    }
}
public enum AttendanceStatus
{
    Present,
    Absent,
    Late,
    Excused
}
public static class EnumExtensions
{
    // هذا التابع يترجم الـ enum إلى نص عربي
    public static string ToArabic(this AttendanceStatus status)
    {
        return status switch
        {
            AttendanceStatus.Present => "حاضر",
            AttendanceStatus.Absent => "غائب",
            AttendanceStatus.Late => "متأخر",
            AttendanceStatus.Excused => "بعذر",
            _ => status.ToString() // في حال وجود قيمة غير متوقعة
        };
    }

    // هذا التابع يعطي كلاس Bootstrap لوني لكل حالة
    public static string GetBootstrapClass(this AttendanceStatus status)
    {
        return status switch
        {
            AttendanceStatus.Present => "btn-success",
            AttendanceStatus.Absent => "btn-danger",
            AttendanceStatus.Late => "btn-warning",
            AttendanceStatus.Excused => "btn-info",
            _ => "btn-secondary"
        };
    }
}