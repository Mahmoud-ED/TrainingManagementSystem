using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http; // For IFormFile
using System; // For DateTime

namespace TrainingManagementSystem.Models.Entities
{
    public class PlanHeader : BaseEntity
    {
        [Required]
        public string Name { get; set; }

        [StringLength(4, MinimumLength = 4)]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Year must be 4 digits.")]
        [Required]
        public string Year { get; set; }

        [Required]
        public string Quarter { get; set; }

        [StringLength(500)] // زدت الطول للوصف
        public string Description { get; set; }

        [NotMapped]
        [Display(Name = "Approval File")]
        public IFormFile? ApprovalFile { get; set; }
        public string? ApprovalFileUrl { get; set; } // لتخزين مسار الملف بعد الرفع

        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        [DataType(DataType.Date)]
        [Required]
        public DateTime DateFrom { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        [DataType(DataType.Date)]
        public DateTime? DateTo { get; set; }
    }
}