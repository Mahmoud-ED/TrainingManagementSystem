using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingManagementSystem.Models.Entities
{

    public class UserProfile : BaseEntity
    {
        [Remote(action: "IsDisplayNameInUse", controller: "Account", AdditionalFields = nameof(Id))]
        public string DisplayName { get; set; }
        public string? ImageUrl { get; set; }
        public bool Gender { get; set; }

        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; }

        [ForeignKey("ApplicationUser")]
        public string? UserId { get; set; }

        [NotMapped]
        public IFormFile? Image { get; set; }

    }
}
