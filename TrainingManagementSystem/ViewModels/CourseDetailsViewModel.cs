// في مجلد ViewModels

// --- لصفحة Details الخاصة بـ CourseDetails ---
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TrainingManagementSystem.Models.Entities;

public class CourseDetailsDisplayViewModel
{
    public Course Course { get; set; } // الكورس الرئيسي
    public Guid Id { get; set; } // CourseDetailsId
    public string CourseName { get; set; } // اسم الكورس الرئيسي
    public Guid ParentCourseId { get; set; } // ID للكورس الرئيسي

    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
    public DateTime StartDate { get; set; }
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
    public DateTime? EndDate { get; set; }
    public int DurationHours { get; set; }
    public string LocationName { get; set; }
    public string CourseTypeName { get; set; }
    public string StatusName { get; set; }

    public List<EnrolledTraineeViewModel> EnrolledTrainees { get; set; } = new List<EnrolledTraineeViewModel>();
}

public class EnrolledTraineeViewModel // لتمثيل كل متدرب مسجل في CourseDetails.Details View
{
    public Guid CourseTraineeId { get; set; } // ID لـ CourseTrainee (مهم للتعديل/الحذف)
    public Guid CourseId { get; set; } // ID لـ CourseId (مهم للتعديل/الحذف)
    public Guid TraineeId { get; set; }
    public string TraineeName { get; set; }
    public string ProfileImgeUrl { get; set; }
    public string? TraineeEmail { get; set; } // أو أي معلومات أخرى للمتدرب
    public decimal? AttendancePercentage { get; set; }
    public decimal? Grade { get; set; }
    public string? CertificateNumber { get; set; }
    public DateTime? CertificateIssueDate { get; set; }
    // يمكنك إضافة رابط الشهادة والملاحظات إذا أردت عرضها مباشرة في القائمة
}

public class CourseDetailsIndexViewModel
{
    // 1. القائمة الأصلية لعرضها في الجداول
    public IEnumerable<CourseDetails> CourseDetailsList { get; set; }

    // 2. بطاقات الإحصائيات (التقرير)
    public CourseDetails NextUpcomingCourse { get; set; }
    public int ActiveCoursesCount { get; set; }
    public int CompletedCoursesCount { get; set; }
    public int UpcomingCoursesCount { get; set; }
    public int TotalRegisteredTrainees { get; set; }
    public int PostponedCoursesCount { get; set; }
    public int CancelledCoursesCount { get; set; }
}

// --- ViewModel لفورم تسجيل متدرب جديد في CourseDetails ---
public class EnrollTraineeViewModel
{
    [Required]
    public Guid CourseDetailsId { get; set; }
    public Guid CourseId { get; set; } // ID لـ CourseId (مهم للتعديل/الحذف)
    public string CourseDetailsName { get; set; } // للعرض في الفورم

    [Required(ErrorMessage = "يجب اختيار متدرب")]
    [Display(Name = "المتدرب")]
    public Guid SelectedTraineeId { get; set; }
    public SelectList AvailableTrainees { get; set; } // قائمة بالمتدربين غير المسجلين بعد في هذه الدورة

    // يمكنك إضافة حقول أولية من CourseTrainee هنا إذا أردت تعبئتها عند التسجيل مباشرة
    // مثل تاريخ بدء افتراضي أو ملاحظات أولية.
    // ولكن عادةً هذه تترك لـ EditEnrollment.
    [Display(Name = "ملاحظات (اختياري)")]
    public string? Notes { get; set; }
}

// --- ViewModel لتعديل بيانات تسجيل متدرب (CourseTrainee) ---
public class EditEnrollmentViewModel
{
    [Required]
    public Guid CourseTraineeId { get; set; } // ID لـ CourseTrainee
    public Guid CourseDetailsId { get; set; } // للتوجيه بعد الحفظ
    public string CourseName { get; set; } // اسم الدورة للعرض
    public string TraineeName { get; set; } // اسم المتدرب للعرض

    [Column(TypeName = "decimal(5,2)")]
    [Display(Name = "نسبة الحضور")]
    [Range(0, 100, ErrorMessage = "النسبة يجب أن تكون بين 0 و 100.")]
    public decimal? AttendancePercentage { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    [Display(Name = "الدرجة")]
    public decimal? Grade { get; set; } // يمكن تعديل النطاق حسب نظام الدرجات

    [MaxLength(100)]
    [Display(Name = "رقم الشهادة")]
    public string? CertificateNumber { get; set; }

    [Display(Name = "رابط الشهادة")]
    [Url]
    public string? CertificateUrl { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "تاريخ إصدار الشهادة")]
    public DateTime? CertificateIssueDate { get; set; }

    [Display(Name = "ملاحظات")]
    public string? Notes { get; set; }
}