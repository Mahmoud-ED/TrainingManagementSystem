using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // For List

namespace TrainingManagementSystem.Models.Entities
{
    public class Status : BaseEntity
    {
        [StringLength(100)]
        [Required]
        public string Name { get; set; }

        public List<CourseDetails> CourseDetails { get; set; } = new List<CourseDetails>();
    }
}