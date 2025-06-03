namespace TrainingManagementSystem.ViewModels
{
    public class TrainerCheckboxViewModel
    {

        public Guid Id { get; set; }    // ID المدرب
        public string Name { get; set; } // اسم المدرب للعرض بجانب الـ checkbox
        public bool IsSelected { get; set; }
    }
}
