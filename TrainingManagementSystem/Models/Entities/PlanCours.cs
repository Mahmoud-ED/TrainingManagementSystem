using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace TrainingManagementSystem.Models.Entities
{
    public class PlanCours : BaseEntity
    {

        [ValidateNever]
        public Course Course { get; set; }
        [Required(ErrorMessage = "الدورة التدريبية مطلوبة")]
        [Display(Name = "الدورة التدريبية")]
        public Guid CourseId { get; set; }
       
    
        [Display(Name = "المدة بالساعات")]
        [Range(1, 1000, ErrorMessage = "المدة بالساعات يجب أن تكون بين 1 و 1000")] // مثال على التحقق
        public int DurationHours { get; set; } // تم تغييره إلى int

        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public int? Numberoftargets { get; set; } // عدد الأهداف، يمكن أن يكون null إذا لم يتم تحديده

        //----------------------------------------------------
        [ValidateNever]
        public Locations? Locations { get; set; }
        [Display(Name = "Locations")]
        public Guid? LocationId { get; set; }
        public List<PlanCoursTrainer> PlanCoursTrainers { get; set; } = new();


    }
}
