using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System; // For DateTime
using System.Collections.Generic; // For List
using System.ComponentModel.DataAnnotations;

namespace TrainingManagementSystem.Models.Entities
{
    public class CourseDetails : BaseEntity
    {
        [ValidateNever]
        public Course Course { get; set; }
        [Required(ErrorMessage = "الدورة التدريبية مطلوبة")]
        [Display(Name = "الدورة التدريبية")]
        public Guid CourseId { get; set; }
        //------------------------------------------------

        public string? Name { get; set; } // اسم الدورة، يمكن أن يكون null إذا لم يتم تحديده

        [Display(Name = "المدة بالساعات")]
        [Range(1, 1000, ErrorMessage = "المدة بالساعات يجب أن تكون بين 1 و 1000")] // مثال على التحقق
        public int DurationHours { get; set; } // تم تغييره إلى int

        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public int?  Numberoftargets { get; set; } // عدد الأهداف، يمكن أن يكون null إذا لم يتم تحديده

        //----------------------------------------------------
        [ValidateNever]
        public Locations? Locations { get; set; }
        [Display(Name = "Locations")]
        public Guid? LocationId { get; set; }

        //----------------------------------------------------
        [ValidateNever]
        public CourseType CourseType { get; set; }
        [Required(ErrorMessage = "النوع مطلوب")]
        [Display(Name = "Course Type")]
        public Guid CourseTypeId { get; set; }

        //----------------------------------------------------
        [ValidateNever]
        public Status Status { get; set; }
        [Required(ErrorMessage = "الحالة مطلوبة")]
        [Display(Name = "Status")]
        public Guid StatusId { get; set; }

        //----------------------------------------------------
        // العلاقة مع المدربين أصبحت Many-to-Many عبر CourseTrainer
        //public List<CourseTrainer> CourseTrainers { get; set; } = new List<CourseTrainer>();

        //----------------------------------------------------
        // العلاقة مع المتدربين المسجلين Many-to-Many عبر CourseTrainee
        public List<CourseTrainee> CourseTrainees { get; set; } = new List<CourseTrainee>();

        public List<CoursDetailsTrainer> CoursDetailsTrainer { get; set; }
    }
}