using System.ComponentModel.DataAnnotations;

namespace TrainingManagementSystem.ViewModels
{

public class AssignTrainerViewModel
    {
        [Required(ErrorMessage = "الرجاء اختيار مدرب من القائمة.")]
        [Display(Name = "المدرب")]
        public Guid SelectedTrainerId { get; set; }

        // سيتم تمريره كحقل مخفي
        public Guid CourseDetailsId { get; set; }

        // لعرض اسم الدورة في الواجهة
        public string CourseDetailsName { get; set; }
    }
}
