using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // For List

namespace TrainingManagementSystem.Models.Entities
{
    public class Level : BaseEntity
    {
        [StringLength(100)]
        [Required]
        public string Name { get; set; }

        public List<Course> Courses { get; set; } = new List<Course>();
    }
}