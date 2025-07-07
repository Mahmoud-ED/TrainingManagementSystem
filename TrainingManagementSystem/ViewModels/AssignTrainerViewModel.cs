using System.ComponentModel.DataAnnotations;

namespace TrainingManagementSystem.ViewModels
{

    using System.ComponentModel.DataAnnotations;

    public class AssignTrainerViewModel
    {
        // الحقول الموجودة حالياً
        public Guid CourseDetailsId { get; set; }
        public string? CourseDetailsName { get; set; }

        [Required(ErrorMessage = "الرجاء اختيار مدرب أو مشرف.")]
        [Display(Name = "ابحث عن المدرب / المشرف بالاسم أو الرقم الوظيفي")]
        public Guid SelectedTrainerId { get; set; }

        // --- الحقل الجديد الذي تمت إضافته ---
        [Required]
        [Display(Name = "الصفة")]
        public bool IsTrainer { get; set; } = true; // true = مدرب, false = مشرف. القيمة الافتراضية هي مدرب
    }

    public class AssignPlanCoursTrainerViewModel
    {
        [Required(ErrorMessage = "الرجاء اختيار مدرب من القائمة.")]
        [Display(Name = "المدرب")]
        public Guid SelectedTrainerId { get; set; }

        public Guid PlanCoursId { get; set; }

        public string Name { get; set; }
    }
}
