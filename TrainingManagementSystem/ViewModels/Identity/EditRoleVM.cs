using System.ComponentModel.DataAnnotations;

namespace TrainingManagementSystem.ViewModels.Identity
{
    public class EditRoleVM
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Please Enter Role Name")]
        [Display(Name ="Role Name")]
        public string RoleName { get; set; }

    }

}
