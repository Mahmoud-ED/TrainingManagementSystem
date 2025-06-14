using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // For List

namespace TrainingManagementSystem.Models.Entities
{
    public class CourseClassification : BaseEntity
    {
        [StringLength(100)]

        [Display(Name = "اسم المحور")]
        [Required(ErrorMessage = "اسم المحور مطلوب")]
        public string Name { get; set; }

        public List<Course> Courses { get; set; } = new List<Course>();
    }
}