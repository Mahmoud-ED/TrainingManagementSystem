using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace TrainingManagementSystem.Models.Entities
{
    public class PlanCoursTrainer : BaseEntity
    {
       
            [Required(ErrorMessage = "Trainer is required")]
            [Display(Name = "Trainer")]
            public Guid TrainerId { get; set; }
            [ValidateNever]
            public Trainer Trainer { get; set; }


            public Guid PlanCoursId { get; set; }
            [ValidateNever]
            public PlanCours PlanCourss { get; set; }



    }
}
