using System;
using System.ComponentModel.DataAnnotations;

public class CertificateViewModel
{
    // بيانات المتدرب
    [Display(Name = "اسم المتدرب")]
    public string TraineeName { get; set; }

    // بيانات الدورة
    [Display(Name = "اسم الدورة")]
    public string CourseName { get; set; }

    [Display(Name = "تاريخ بدء الدورة")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
    public DateTime CourseStartDate { get; set; }

    [Display(Name = "تاريخ انتهاء الدورة")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
    public DateTime? CourseEndDate { get; set; }

    [Display(Name = "مدة الدورة (ساعات)")]
    public int CourseDurationHours { get; set; }

    [Display(Name = "العدد المستهدف")]
    public int? CourseNumberoftargets { get; set; }


    [Display(Name = "موقع انعقاد الدورة")]
    public string CourseLocation { get; set; }


    // بيانات الشهادة نفسها
    [Display(Name = "رقم الشهادة")]
    public string? CertificateNumber { get; set; }

    [Display(Name = "تاريخ إصدار الشهادة")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
    public DateTime? CertificateIssueDate { get; set; }

    [Display(Name = "التقدير")]
    public string? Grade { get; set; } // قد يكون Grade رقم أو نص (ممتاز، جيد جداً)

    // نصوص ثابتة يمكن تخصيصها
    public string IssuingOrganizationName { get; set; }  
    public string CertificateTitle { get; set; } = "شهادة إتمام دورة تدريبية";
    public string PresentedToText { get; set; } = "تشهد إدارة التدريب بأن المتدرب/ـة:";
    public string CompletedCourseText { get; set; } = "قد أتم بنجاح متطلبات البرنامج التدريبي في:";
    public string CoursePeriodText { get; set; } = "المنعقد خلال الفترة من";
    public string ToText { get; set; } = "إلى";
    public string ForDurationText { get; set; } = "بواقع";
    public string HoursText { get; set; } = "ساعة تدريبية معتمدة.";
    public string AndAchievedGradeText { get; set; } = "وحصل على تقدير";
    public string IssuedOnText { get; set; } = "صدرت هذه الشهادة بتاريخ:";
    public string Signature1Title { get; set; } = "مدير التدريب";
    public string Signature2Title { get; set; } = "المدير العام"; // أو الختم

    // مسارات للصور (اختياري، يمكن أن تكون جزء من CSS الخلفية)
    public string LogoUrl { get; set; } = "/img/oo.png"; // ضع مسار شعارك هنا
    public string StampUrl { get; set; } = "/images/stamp_placeholder.png"; // ضع مسار ختمك هنا
}