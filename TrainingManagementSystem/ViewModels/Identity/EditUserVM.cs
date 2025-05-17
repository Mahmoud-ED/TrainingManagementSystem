using System.ComponentModel.DataAnnotations;

namespace TrainingManagementSystem.ViewModels.Identity
{
    public class EditUserVM
    {
        public EditUserVM()
        {
            Roles = new List<string>();
            Claims = new List<string>();
        }
        public string Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public int Age { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd hh:mm:ss tt}")]
        public DateTimeOffset? LockoutEnd { get; set; }

        public bool EmailConfirmed { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? LastAccessTime { get; set; }

        public List<string> Roles { get; set; }
        public List<string> Claims { get; set; }

        public string? DisplayName { get; set; }

        public string CreatedDateLocalTime
        {
            get { return CreatedDate.ToLocalTime().ToString("dd-MM-yyyy hh:mm:ss tt"); }
        }
        public string ModifiedDateLocalTime
        {
            get { return ModifiedDate?.ToLocalTime().ToString("dd-MM-yyyy hh:mm:ss tt"); }
        }
        public string LastAccessTimeLocalTime
        {
            get { return LastAccessTime?.ToLocalTime().ToString("dd-MM-yyyy hh:mm:ss tt"); }
        }
        public string LockoutEndTimeLocalTime
        {
            get { return LockoutEnd?.ToLocalTime().ToString("dd-MM-yyyy hh:mm:ss tt"); }
        }


    }
}
