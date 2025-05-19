using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TrainingManagementSystem.ViewModels
{
    public class TrainerVM
    {
        public Guid Id { get; set; }

        [StringLength(100)]
        [Required(ErrorMessage = "الاسم بالعربية مطلوب")]
        [Display(Name = "الاسم (بالعربية)")]
        public string ArName { get; set; }

        [StringLength(100)]
        [Display(Name = "الاسم (بالإنجليزية)")]
        public string EnName { get; set; }

        [RegularExpression(@"^\d{10,14}$", ErrorMessage = "رقم الهاتف يجب أن يتكون من 10 إلى 14 رقمًا.")]
        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Display(Name = "رقم الهاتف")]
        public string PhoneNo { get; set; }

        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; }

        [StringLength(10, MinimumLength = 10, ErrorMessage = "الرقم الوطني يجب أن يتكون من 10 أرقام.")]
        [RegularExpression(@"^[12]\d{9}$", ErrorMessage = "صيغة الرقم الوطني غير صحيحة.")]
        [Display(Name = "الرقم الوطني")]
        public string NationalNo { get; set; }

        [StringLength(100)]
        [Display(Name = "جهة العمل")]
        public string Employer { get; set; }

        [Required(ErrorMessage = "المؤهل مطلوب")]
        [Display(Name = "المؤهل")]
        public Guid QualificationId { get; set; }

        [Display(Name = "السيرة الذاتية (ملف)")]
        public IFormFile? CVFile { get; set; }
        public string? CVUrl { get; set; }

        [Display(Name = "الصورة الشخصية (ملف)")]
        public IFormFile? ProfileImageFile { get; set; }
        public string? ProfileImageUrl { get; set; }

        [Display(Name = "التخصصات")]
        public List<Guid> SelectedSpecializationIds { get; set; } = new List<Guid>();

        public SelectList QualificationsList { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());
        public MultiSelectList SpecializationsList { get; set; } = new MultiSelectList(Enumerable.Empty<SelectListItem>());

        public string? UserId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        public TrainerVM()
        {
            CreatedAt = DateTime.Now;
        }
    }
}
