using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace TrainingManagementSystem.Models.Entities
{
    public class CourseTrainer : BaseEntity // Join table: Trainer (Many) <-> CourseDetails (Many)
    {
        [Required(ErrorMessage = "Trainer is required")]
        [Display(Name = "Trainer")]
        public Guid TrainerId { get; set; }
        [ValidateNever]
        public Trainer Trainer { get; set; }


        public Guid CourseId { get; set; }
        [ValidateNever]
        public Course Course { get; set; }
    }
}