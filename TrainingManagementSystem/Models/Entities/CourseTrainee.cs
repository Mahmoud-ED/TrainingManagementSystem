using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System; // For DateTime

namespace TrainingManagementSystem.Models.Entities
{
    public class CourseTrainee : BaseEntity // Join table: Trainee (Many) <-> CourseDetails (Many)
    {
        [Required]
        public Guid TraineeId { get; set; }
        [ValidateNever]
        public Trainee Trainee { get; set; }

        [Required]
        public Guid CourseDetailsId { get; set; }
        [ValidateNever]
        public CourseDetails CourseDetails { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Attendance Percentage")]
        [Range(0, 100, ErrorMessage = "Percentage must be between 0 and 100.")]
        public decimal? AttendancePercentage { get; set; }

        [Column(TypeName = "decimal(5,2)")] // أو float/double إذا كانت الدرجات تحتاج دقة أكبر
        [Display(Name = "Grade")]
        public decimal? Grade { get; set; }

        [MaxLength(100)]
        [Display(Name = "Certificate Number")]
        public string? CertificateNumber { get; set; }

        [Display(Name = "Certificate URL")]
        [Url]
        public string? CertificateUrl { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Certificate Issue Date")]
        public DateTime? CertificateIssueDate { get; set; }

        public string? Notes { get; set; }
    }
}