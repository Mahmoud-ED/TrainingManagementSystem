// ViewModel لشاشة العرض الرئيسية (المصفوفة)
using System.ComponentModel.DataAnnotations;
using TrainingManagementSystem.Models.Entities;
public class CourseAttendanceIndexViewModel
{
    public Guid CourseDetailsId { get; set; }
    public string CourseName { get; set; }
    public List<TraineeInfo> Trainees { get; set; }
    public List<DateTime> DatesInRange { get; set; }
    // نستخدم Dictionary لسهولة وسرعة الوصول للحالة في الـ View
    // Key: (TraineeId, Date), Value: Attendance record
    public Dictionary<(Guid, DateTime), Attendance> AttendanceRecords { get; set; }
}

// ViewModel لشاشة تسجيل الحضور اليومي
public class DailyAttendanceViewModel
{
    public Guid CourseDetailsId { get; set; }
    public string CourseName { get; set; }
    [DataType(DataType.Date)]
    public DateTime AttendanceDate { get; set; }
    public List<TraineeAttendanceItem> TraineeItems { get; set; }
}

// يمثل كل متدرب في شاشة التسجيل اليومي
public class TraineeAttendanceItem
{
    public Guid TraineeId { get; set; }
    public string TraineeName { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Notes { get; set; }
}

// ViewModel لطباعة الكشف
public class PrintableSheetViewModel
{
    public string CourseName { get; set; }
    public string InstructorName { get; set; } // يمكن إضافتها
    public DateTime PrintDate { get; set; }
    public List<TraineeInfo> Trainees { get; set; }
}

// معلومات مختصرة عن المتدرب
public class TraineeInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}