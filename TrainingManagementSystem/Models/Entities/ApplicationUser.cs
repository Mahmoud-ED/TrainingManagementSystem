using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using TrainingManagementSystem.Models.Entities;

namespace TrainingManagementSystem.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Range(18, 100)]
        public int Age { get; set; }
        public DateTime? LastAccessTime { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public bool? Approval { get; set; }


        [ValidateNever]
        public UserProfile UserProfile { get; set; }

        [ValidateNever]
        public Employee Employee { get; set; }
        
        [ValidateNever]
        public Trainer Trainer { get; set; }
        
        [ValidateNever]
        public Trainee Trainee { get; set; }

        public string LastAccessTimeLocalTime
        {
            get { return LastAccessTime?.ToLocalTime().ToString("dd-MM-yyyy hh:mm:ss tt"); }
        }
        public string CreatedDateLocalTime
        {
            get { return CreatedDate.ToLocalTime().ToString("dd-MM-yyyy hh:mm:ss tt"); }
        }
        public string ModifiedDateLocalTime
        {
            get { return ModifiedDate?.ToLocalTime().ToString("dd-MM-yyyy hh:mm:ss tt"); }
        }

    }
}
