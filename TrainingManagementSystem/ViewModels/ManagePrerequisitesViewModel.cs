using TrainingManagementSystem.Models.Entities;

namespace TrainingManagementSystem.ViewModels
{
    public class ManagePrerequisitesViewModel
    {
        public Course Course { get; set; }
        public List<Course> CurrentPrerequisites { get; set; }
        public List<Course> AvailableCourses { get; set; }
    }
}
