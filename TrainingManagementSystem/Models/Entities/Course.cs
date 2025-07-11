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
        public Level? Level { get; set; } // مفرد
        [Display(Name = "المستوى")]
        public Guid? LevelId { get; set; }

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

        public List<PlanCours> PlanCours { get; set; } = new List<PlanCours>();

        //---------------------------------------------------------------

        [ValidateNever]
        public CourseParent? CourseParent { get; set; } // مفرد و nullable
        [Display(Name = "مرجع الدورة السابقة")]
        public Guid? CourseParentId { get; set; } //Refrence of the previous course





        public int DurationHours { get; set; }

        public int? Days { get; set; }


        // خاصية التنقل: قائمة بالكورسات التي يعتبر هذا الكورس متطلباً لها
        public virtual ICollection<CoursePrerequisite> PrerequisitesFor { get; set; }

        // خاصية التنقل: قائمة بالمتطلبات المسبقة لهذا الكورس
        public virtual ICollection<CoursePrerequisite> RequiredPrerequisites { get; set; }

        

        public Course()
        {
            PrerequisitesFor = new HashSet<CoursePrerequisite>();
            RequiredPrerequisites = new HashSet<CoursePrerequisite>();
        }

    }
}