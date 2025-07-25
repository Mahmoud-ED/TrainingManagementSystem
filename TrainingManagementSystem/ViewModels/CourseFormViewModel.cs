﻿// في مجلد ViewModels

// ViewModel لـ CourseDetails داخل الفورم الرئيسي
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using TrainingManagementSystem.Models.Entities;
using TrainingManagementSystem.ViewModels; 
public class CourseDetailFormEntryViewModel
{
    // لا نحتاج ID هنا في حالة الإنشاء، سيتم إنشاؤه في السيرفر
    // public Guid Id { get; set; } // يمكن إضافته إذا كنت ستستخدم نفس الـ VM للتعديل لاحقاً

    [Display(Name = "المدة (ساعات)")]
    [Range(1, 1000, ErrorMessage = "المدة يجب أن تكون بين 1 و 1000 ساعة.")]
    [Required(ErrorMessage = "المدة مطلوبة")]
    public int DurationHours { get; set; }
    public int Numberoftargets { get; set; }

    public string Name { get; set; }

    [Display(Name = "تاريخ البدء")]
    [DataType(DataType.Date)]
    [Required(ErrorMessage = "تاريخ البدء مطلوب")]
    public DateTime StartDate { get; set; } = DateTime.Today; // قيمة افتراضية

    [Display(Name = "تاريخ الانتهاء (اختياري)")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [Display(Name = "الموقع")]
    public Guid? LocationId { get; set; }

    [Display(Name = "نوع الدورة")]
    public Guid CourseTypeId { get; set; }

    [Display(Name = "الحالة")]
    [Required(ErrorMessage = "الحالة مطلوبة")]
    public Guid StatusId { get; set; }

    // قوائم منسدلة خاصة بكل عنصر تفاصيل (سيتم ملؤها من الكنترولر أو تمريرها)
    // هذه يمكن أن تكون مكررة إذا كان لديك العديد من CourseDetails،
    // يمكن تحسينها بتمريرها مرة واحدة في الـ ViewModel الرئيسي واستخدامها في الـ View بواسطة JavaScript.
    // لكن للتبسيط الأولي، يمكن وضعها هنا.
    // [ValidateNever]
    // public SelectList Locations { get; set; }
    // [ValidateNever]
    // public SelectList CourseTypes { get; set; }
    // [ValidateNever]
    // public SelectList Statuses { get; set; }
}

public class CourseFormViewModel // (نفس الـ ViewModel السابق مع تعديلات)
{
    public Guid Id { get; set; }

    [StringLength(100)]
    [Required]
    public string Name { get; set; }
    public int Numberoftargets { get; set; }
    
    [StringLength(50)]
    public string Code { get; set; }

    [StringLength(500)]
    public string Description { get; set; }

    [Required(ErrorMessage = "CourseClassification is required")]
    [Display(Name = "تصنيف الدورة")]
    public Guid CourseClassificationId { get; set; }
    public Course Course { get; set; }
    public List<Course> CourseList { get; set; }

    [Required(ErrorMessage = "Level is required")]
    [Display(Name = "المستوى")]
    public Guid LevelId { get; set; }

    [Display(Name = "مرجع الدورة السابقة")]
    public Guid? CourseParentId { get; set; }

    public List<SelectListItem> CourseClassifications { get; set; } = new();
    public List<SelectListItem> Levels { get; set; } = new();
    public List<SelectListItem> CourseParents { get; set; } = new();


    // لإدارة
    // (كما كان)
    [Display(Name = "المدربون")]
    public List<Guid> SelectedTrainerIds { get; set; } = new List<Guid>();
    public List<TrainerCheckboxViewModel> AvailableTrainers { get; set; }

    // ---- الجديد: قائمة بتفاصيل الدورة ----
    [Display(Name = "تفاصيل تنفيذ الدورة")]
    public List<CourseDetailFormEntryViewModel> CourseDetailsEntries { get; set; } = new List<CourseDetailFormEntryViewModel>();

    // ---- الجديد: قوائم منسدلة لـ CourseDetails ----
    // يتم تمريرها مرة واحدة لتستخدمها جميع إدخالات CourseDetails في الواجهة
    [ValidateNever]
    // ⬇️ أيضاً يُفضل تغييرهم لنفس السبب
    public List<SelectListItem> Locations { get; set; } = new();
    [ValidateNever]
    public List<SelectListItem> CourseTypes { get; set; } = new();
    [ValidateNever]
    public List<SelectListItem> Statuses { get; set; } = new();
}