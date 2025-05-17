using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // For List

namespace TrainingManagementSystem.Models.Entities
{
    public class Course : BaseEntity
    {
        [ValidateNever]
        public CourseClassification CourseClassification { get; set; } // مفرد
        [Required(ErrorMessage = "CourseClassification is required")]
        [Display(Name = "تصنيف الدورة")]
        public Guid CourseClassificationId { get; set; }

        //---------------------------------------------------------------
        [ValidateNever]
        public Level Level { get; set; } // مفرد
        [Required(ErrorMessage = "Level is required")] // تم تصحيح رسالة الخطأ
        [Display(Name = "المستوى")]
        public Guid LevelId { get; set; }

        //---------------------------------------------------------------
        [StringLength(100)]
        [Required]
        public string Name { get; set; }

        [StringLength(50)]
        public string Code { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        //---------------------------------------------------------------
        public List<CourseDetails> CourseDetails { get; set; } 
        //---------------------------------------------------------------
          public List<CourseTrainer> CourseTrainers { get; set; }
        //---------------------------------------------------------------

        [ValidateNever]
        public CourseParent? CourseParent { get; set; } // مفرد و nullable
        [Display(Name = "مرجع الدورة السابقة")]
        public Guid? CourseParentId { get; set; } //Refrence of the previous course
    }
}