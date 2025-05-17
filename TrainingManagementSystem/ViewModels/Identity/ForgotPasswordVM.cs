using System.ComponentModel.DataAnnotations;

namespace TrainingManagementSystem.ViewModels.Identity
{
    public class ForgotPasswordVM
    {
        [EmailAddress]
        public required string Email { get; set; }

    }
}
