using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TrainingManagementSystem.ViewModels
{
    public class CreateCourseViewModel
    {
        // ... (كل الخصائص السابقة: Name, Code, LevelId, etc.)
        [Required(ErrorMessage = "اسم الدورة مطلوب")]
        [Display(Name = "اسم الدورة")]
        public string Name { get; set; }

        [Required(ErrorMessage = "كود الدورة مطلوب")]
        [Display(Name = "كود الدورة")]
        public string Code { get; set; }

        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "يجب اختيار محور الدورة")]
        [Display(Name = "محور الدورة")]
        public Guid CourseClassificationId { get; set; }

        [Required(ErrorMessage = "يجب اختيار مستوى الدورة")]
        [Display(Name = "مستوى الدورة")]
        public Guid LevelId { get; set; }

        [Display(Name = "الدورة المرجعية (إن وجدت)")]
        public Guid? CourseParentId { get; set; }

        // --- الإضافة الجديدة هنا ---
        [Display(Name = "المدربون")]
        // هذه ستحتوي على IDs المدربين الذين يختارهم المستخدم من الفورم
        public List<Guid> SelectedTrainerIds { get; set; } = new List<Guid>();

        // --- وهذه ستحمل قائمة كل المدربين لعرضها في الفورم ---
        public IEnumerable<SelectListItem>? AllTrainers { get; set; }


        // ... (باقي خصائص القوائم المنسدلة)
        public IEnumerable<SelectListItem>? CourseClassifications { get; set; }
        public IEnumerable<SelectListItem>? Levels { get; set; }
        public IEnumerable<SelectListItem>? ParentCourses { get; set; }
    }
}
