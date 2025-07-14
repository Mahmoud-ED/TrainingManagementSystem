// في مجلد ViewModels (أو حيث تضع ViewModels)
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TrainingManagementSystem.Models.Entities; // تأكد من المسار الصحيح

namespace TrainingManagementSystem.ViewModels
{
    public class TraineeVM
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "الاسم باللغة العربية مطلوب")]
        [StringLength(100)]
        [Display(Name = "الاسم (عربي)")]
        public string ArName { get; set; }


        public int Year { get; set; }
        public string? NUM { get; set; }

        [StringLength(100)]
        [Display(Name = "الاسم (إنجليزي)")]
        public string? EnName { get; set; }

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [RegularExpression(@"^\d{9,15}$", ErrorMessage = "رقم الهاتف يجب أن يتكون من 9 إلى 15 رقمًا")]
        [Display(Name = "رقم الهاتف")]
        public string PhoneNo { get; set; }

        [StringLength(20)]
        [Display(Name = "الرقم الوطني")]
        public string? NationalNo { get; set; }

        [Display(Name = "المؤسسة")]
        public Guid? OrganizationId { get; set; }

        [Display(Name = "القسم")]
        public Guid? DepartmentId { get; set; }

        [Required(ErrorMessage = "التخصص مطلوب")]
        [Display(Name = "التخصص")]
        public Guid? SpecializationId { get; set; }

        [Required(ErrorMessage = "المؤهل العلمي مطلوب")]
        [Display(Name = "المؤهل العلمي")]
        public Guid QualificationId { get; set; }

        [StringLength(250)]
        [Display(Name = "العنوان")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [StringLength(100)]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; }

        [Display(Name = "حساب المستخدم")]
        public string? UserId { get; set; } // لربط المتدرب بحساب مستخدم

        // لحفظ الدورات المختارة (إذا كنت ستديرها من هنا)
        public List<Guid>? SelectedCourseIds { get; set; }

        // ملفات (إذا كان للمتدرب صورة شخصية أو سيرة ذاتية)
        [Display(Name = "الصورة الشخصية")]
        public IFormFile? Imge { get; set; }
        public string? ProfileImageUrl { get; set; }

        // قوائم الاختيار للـ Dropdowns
        public SelectList? OrganizationsList { get; set; }
        public SelectList? DepartmentsList { get; set; } // قد يتم تعبئتها بناءً على المؤسسة المختارة
        public SelectList? SpecializationsList { get; set; }
        public SelectList? QualificationsList { get; set; }
        public SelectList? UsersList { get; set; } // قائمة بالمستخدمين غير المرتبطين بمتدربين بعد (اختياري)

        public DateTime CreatedAt { get; set; } // لعرض تاريخ الإنشاء
    }
}