using System.ComponentModel.DataAnnotations;
using TrainingManagementSystem.Models.Entities;

public class Specialization : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    // 🔗 العلاقة عبر الجدول الوسيط فقط
    public List<TrainerSpecialization> TrainerSpecializations { get; set; } = new();
    public List<Trainee> Trainees { get; set; } = new();
}
