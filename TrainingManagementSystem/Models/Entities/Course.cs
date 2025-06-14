using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // For List

namespace TrainingManagementSystem.Models.Entities
{
    public class Course : BaseEntity
    {
        [ValidateNever]
        public CourseClassification CourseClassification { get; set; } // مفرد
        [Required(ErrorMessage = "تصنيف الدورة مطلوب")]
        [Display(Name = "محور التصنيف")]
        public Guid CourseClassificationId { get; set; }

        //---------------------------------------------------------------
        [ValidateNever]
        public Level Level { get; set; } // مفرد
        [Required(ErrorMessage = "المستوى مطلوب")] // تم تصحيح رسالة الخطأ
        [Display(Name = "المستوى")]
        public Guid LevelId { get; set; }

        //---------------------------------------------------------------
        [StringLength(100)]
        [Required]
        [Display(Name = "الاسم")]
        public string Name { get; set; }

        [StringLength(50)]
        [Display(Name = "الرمز")]
        public string Code { get; set; }

        [StringLength(500)]
        [Display(Name = "الوصف")]
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