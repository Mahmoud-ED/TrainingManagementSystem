using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using TrainingManagementSystem.Models.Entities;

public class Trainer : BaseEntity
{
    [StringLength(100)]
    [Required]
    public string ArName { get; set; }

    [StringLength(100)]
    public string EnName { get; set; }

    [RegularExpression(@"^\d{10,14}$", ErrorMessage = "The phone number must consist of 10 to 14 digits.")]
    [Required]
    public string PhoneNo { get; set; }

    [EmailAddress]
    [Required]
    public string Email { get; set; }

    [StringLength(10, MinimumLength = 10, ErrorMessage = "National ID must be 10 digits.")]
    [RegularExpression(@"^[12]\d{9}$", ErrorMessage = "National ID format is invalid.")]
    public string NationalNo { get; set; }

    [StringLength(100)]
    public string Employer { get; set; }

    [Required]
    public Guid QualificationId { get; set; }
    [ValidateNever]
    public Qualification Qualification { get; set; }

    // 🔗 العلاقة عبر الجدول الوسيط فقط
    public List<TrainerSpecialization> TrainerSpecializations { get; set; } = new();

    public List<CourseTrainer> CourseTrainers { get; set; } = new();

    public string? UserId { get; set; }
    [ValidateNever]
    public ApplicationUser? ApplicationUser { get; set; }

    [NotMapped]
    [Display(Name = "CV File")]
    public IFormFile? CV { get; set; }
    public string? CVUrl { get; set; }
    public string? ProfileImageUrl { get; set; }

    [NotMapped]
    public IFormFile? Image { get; set; }

}
