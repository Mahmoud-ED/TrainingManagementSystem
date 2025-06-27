using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // For List

namespace TrainingManagementSystem.Models.Entities
{
    public class Locations : BaseEntity
    {
        [StringLength(100)]
        [Required]
        public string Name { get; set; }

        public int? FullCount { get; set; }
        public List<CourseDetails> CourseDetails { get; set; } = new List<CourseDetails>();
        public List<PlanCours> PlanCours { get; set; } = new List<PlanCours>();
    }
}