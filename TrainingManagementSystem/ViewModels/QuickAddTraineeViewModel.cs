namespace TrainingManagementSystem.ViewModels
{
    using Microsoft.AspNetCore.Mvc.Rendering;
    using System.ComponentModel.DataAnnotations;

    // ViewModel للنموذج المنبثق (Modal)
    public class QuickAddTraineeViewModel
    {
        // هذا الحقل ضروري لربط التسجيل بالدورة الحالية
        public Guid CourseDetailsId { get; set; }

        [Required(ErrorMessage = "اسم المتدرب مطلوب.")]
        [StringLength(100)]
        [Display(Name = "اسم المتدرب")]
        public string ArName { get; set; }

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب.")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة.")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; }

        // قيمة حقل الهاتف مطلوبة في الموديل الأصلي
        [Required(ErrorMessage = "رقم الهاتف مطلوب.")]
        [RegularExpression(@"^\d{10,14}$", ErrorMessage = "يجب أن يتكون رقم الهاتف من 10 إلى 14 رقمًا.")]
        [Display(Name = "رقم الهاتف")]
        public string PhoneNo { get; set; }

        [Required(ErrorMessage = "الجهة مطلوبة.")]
        [Display(Name = "الجهة")]
        public Guid OrganizationId { get; set; }

        [Required(ErrorMessage = "المؤهل العلمي مطلوب.")]
        [Display(Name = "المؤهل العلمي")]
        public Guid QualificationId { get; set; }

        public SelectList? AvailableOrganizations { get; set; }
        public SelectList? AvailableQualifications { get; set; }
    }

    // ViewModel الرئيسي للصفحة
    public class EnrollMultipleTraineesViewModel
    {
        public Guid CourseDetailsId { get; set; }
        public string CourseDetailsName { get; set; }

        [Display(Name = "اختر الجهة للفلترة")]
        public Guid? SelectedOrganizationId { get; set; }
        public SelectList AvailableOrganizations { get; set; }

        public List<TraineeSelectionItem> Trainees { get; set; } = new List<TraineeSelectionItem>();

        [Required(ErrorMessage = "يجب اختيار متدرب واحد على الأقل.")]
        public List<Guid> SelectedTraineeIds { get; set; } = new List<Guid>();

        // إضافة النموذج الجديد هنا
        public QuickAddTraineeViewModel NewTrainee { get; set; } = new QuickAddTraineeViewModel();
    }

    public class TraineeSelectionItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string NationalId { get; set; }
        public Guid? OrganizationId { get; set; }
        public string OrganizationName { get; set; }
    }
}
