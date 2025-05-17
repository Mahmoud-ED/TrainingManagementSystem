using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // For List

namespace TrainingManagementSystem.Models.Entities
{
    public class CourseParent : BaseEntity
    {
        [StringLength(200)]
        [Required]
        public string Name { get; set; }

        public List<Course> Courses { get; set; } = new List<Course>();
    }
}