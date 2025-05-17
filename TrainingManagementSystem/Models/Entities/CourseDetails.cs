using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // For List
using System; // For DateTime

namespace TrainingManagementSystem.Models.Entities
{
    public class CourseDetails : BaseEntity
    {
        [ValidateNever]
        public Course Course { get; set; }
        [Required(ErrorMessage = "Course is required")]
        [Display(Name = "Course")]
        public Guid CourseId { get; set; }
        //------------------------------------------------

        [Display(Name = "Duration (Hours)")]
        [Range(1, 1000, ErrorMessage = "Duration must be between 1 and 1000 hours.")] // مثال على التحقق
        public int DurationHours { get; set; } // تم تغييره إلى int

        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        //----------------------------------------------------
        [ValidateNever]
        public Location Location { get; set; }
        [Required(ErrorMessage = "Location is required")]
        [Display(Name = "Location")]
        public Guid LocationId { get; set; }

        //----------------------------------------------------
        [ValidateNever]
        public CourseType CourseType { get; set; }
        [Required(ErrorMessage = "CourseType is required")]
        [Display(Name = "Course Type")]
        public Guid CourseTypeId { get; set; }

        //----------------------------------------------------
        [ValidateNever]
        public Status Status { get; set; }
        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Status")]
        public Guid StatusId { get; set; }

        //----------------------------------------------------
        // العلاقة مع المدربين أصبحت Many-to-Many عبر CourseTrainer
        //public List<CourseTrainer> CourseTrainers { get; set; } = new List<CourseTrainer>();

        //----------------------------------------------------
        // العلاقة مع المتدربين المسجلين Many-to-Many عبر CourseTrainee
        public List<CourseTrainee> CourseTrainees { get; set; } = new List<CourseTrainee>();
    }
}