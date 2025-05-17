using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // For List

namespace TrainingManagementSystem.Models.Entities
{
    public class Qualification : BaseEntity
    {
        [StringLength(100)]
        [Required]
        public string Name { get; set; }

        // كل متدرب له مؤهل واحد (FK في Trainee)
        public List<Trainee> Trainees { get; set; } = new List<Trainee>();

        // كل مدرب له مؤهل واحد (FK في Trainer)
        public List<Trainer> Trainers { get; set; } = new List<Trainer>();
    }
}