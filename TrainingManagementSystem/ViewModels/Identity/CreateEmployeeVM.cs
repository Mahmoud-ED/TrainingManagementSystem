using Microsoft.AspNetCore.Mvc;
using TrainingManagementSystem.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace TrainingManagementSystem.ViewModels.Identity
{
    public class CreateEmployeeVM
    {
        [DataType(DataType.EmailAddress)]
        [Remote(action: "IsEmailInUse", controller: "Account")]
        public required string Email { get; set; }

        public int Age { get; set; }
        public Employee Employee { get; set; }

    }
}
