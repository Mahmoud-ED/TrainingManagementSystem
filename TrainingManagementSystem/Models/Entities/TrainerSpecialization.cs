using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace TrainingManagementSystem.Models.Entities
{
    public class TrainerSpecialization : BaseEntity
    {
        [Required]
        public Guid TrainerId { get; set; }
        [ValidateNever]
        public Trainer Trainer { get; set; }

        [Required]
        public Guid SpecializationId { get; set; }
        [ValidateNever]
        public Specialization Specialization { get; set; }
    }
}
