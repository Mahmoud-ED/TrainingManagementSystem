using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // For List

namespace TrainingManagementSystem.Models.Entities
{
    public class Category : BaseEntity
    {
        [StringLength(100)]
        [Required] // اسم الفئة عادة مطلوب
        public string Name { get; set; }

        public List<Organizition> Organizations { get; set; }
    }
}