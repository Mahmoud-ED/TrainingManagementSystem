using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; // For List
using Microsoft.AspNetCore.Http; // For IFormFile if you had one, not present here.

// افترض أن ApplicationUser معرّف في مكان ما
// using YourProject.Identity;

namespace TrainingManagementSystem.Models.Entities
{
    public class Trainee : BaseEntity
    {
        [StringLength(100)]
        [Required]
        public string ArName { get; set; }


        [StringLength(100)]
        public string? NUM { get; set; }


        [StringLength(100)]
        public string EnName { get; set; } // قد يكون اختياريًا

        [RegularExpression(@"^\d{10,14}$", ErrorMessage = "The phone number must consist of 10 to 14 digits.")] // مدى مرن أكثر
        [Required]
        public string PhoneNo { get; set; }

        [StringLength(10, MinimumLength = 10, ErrorMessage = "National ID must be 10 digits.")] // مثال للسعودية
        [RegularExpression(@"^[12]\d{9}$", ErrorMessage = "National ID format is invalid.")] // مثال للسعودية
        public string NationalNo { get; set; }


        [ValidateNever]
        public Organizition Organizition { get; set; }
        // [Required(ErrorMessage = "Organization is required")] // فك التعليق إذا كان إلزاميًا
        [Display(Name = "المؤسسة")]
        public Guid? OrganizationId { get; set; } // جعله nullable إذا لم يكن إلزاميًا دائمًا


        [ValidateNever]
        public Department Department { get; set; }
        // [Required(ErrorMessage = "Department is required")] // فك التعليق إذا كان إلزاميًا
        [Display(Name = "القسم")]
        public Guid? DepartmentId { get; set; } // جعله nullable


        [ValidateNever]
        public Specialization? Specialization { get; set; }
        [Required(ErrorMessage = "Specialization is required")]
        [Display(Name = "التخصص")]
        public Guid? SpecializationId { get; set; }


        [ValidateNever]
        public Qualification Qualification { get; set; }
        [Required(ErrorMessage = "Qualification is required")]
        [Display(Name = "المؤهل العلمي")]
        public Guid QualificationId { get; set; }

        public int Year { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [EmailAddress]
        [Required]
        public string Email { get; set; }

        // العلاقة مع الدورات المسجل بها Many-to-Many عبر CourseTrainee
        public List<CourseTrainee> CourseTrainees { get; set; } = new List<CourseTrainee>();

        // العلاقة مع حساب المستخدم
        [ValidateNever]
        public ApplicationUser? ApplicationUser { get; set; } // افترض أنه nullable
        [ForeignKey("ApplicationUser")]
        public string? UserId { get; set; } // مفتاح أجنبي لحساب المستخدم

        public string? ProfileImageUrl { get; set; }

        [NotMapped]
        public IFormFile? Image { get; set; }

    }
}