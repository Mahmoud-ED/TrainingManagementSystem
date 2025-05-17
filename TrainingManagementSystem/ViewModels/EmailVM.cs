using TrainingManagementSystem.Classes;
using System.ComponentModel.DataAnnotations;

namespace TrainingManagementSystem.ViewModels
{
    public class EmailVM
    {
        public MailSettings MailSettings { get; set; }

        [DataType(DataType.EmailAddress, ErrorMessage = "Invalid email")]
        public string To { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
        public IFormFileCollection? Attachments { get; set; } // عدة ملفات

        //public List<IFormFile> Attachments { get; set; }
    }
}
