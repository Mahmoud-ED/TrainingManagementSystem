using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TrainingManagementSystem.Models.Entities
{
    public class Employee : BaseEntity
    {
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(100)]
        public string NotionalNo { get; set; }

        [RegularExpression(@"^\d{14}$", ErrorMessage = "The phone number must consist of 14 digits.")]
        public string PhoneNumber { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [Range(0, 50, ErrorMessage = "Years of experience must be between 0 and 50")]
        public int YearsOfExperience { get; set; }

        [StringLength(100)]
        public string Specialization { get; set; }

        [StringLength(500, ErrorMessage = "Bio cannot be longer than 500 characters")]
        public string Bio { get; set; }

        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; }

        [ForeignKey("ApplicationUser")]
        public string? UserId { get; set; }
    }
}
