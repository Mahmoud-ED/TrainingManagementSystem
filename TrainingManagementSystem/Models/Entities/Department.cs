using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // For List

namespace TrainingManagementSystem.Models.Entities
{
    public class Department : BaseEntity
    {
        [StringLength(100)]
        [Required]
        [Display(Name = "اسم القسم")] 
        public string Name { get; set; }

        public List<Trainee> Trainees { get; set; } = new List<Trainee>();
    }
}