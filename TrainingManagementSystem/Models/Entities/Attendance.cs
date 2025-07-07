using System.ComponentModel.DataAnnotations;
namespace TrainingManagementSystem.Models.Entities
{
    public class Attendance : BaseEntity
    {
        [Required]
        public Guid CourseDetailsId { get; set; }
        public CourseDetails CourseDetails { get; set; }

        [Required]
        public Guid TraineeId { get; set; }
        public Trainee Trainee { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime AttendanceDate { get; set; }

        [Required]
        public AttendanceStatus Status { get; set; } // ????/????/?????/????

        [MaxLength(250)]
        public string? Notes { get; set; }
    }
}
public enum AttendanceStatus
{
    Present,
    Absent,
    Late,
    Excused
}