using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Collections.Specialized.BitVector32;

namespace TrainingManagementSystem.Models.Entities
{
    public class Organizition : BaseEntity
    {
        [StringLength(200)]
        public string Name { get; set; }


        [StringLength(100)]
        public string NUM { get; set; }

        //-----------------------------------------------------------
        [ValidateNever]
        public Category? Category { get; set; }

        public Guid? CategoryId { get; set; }

        //-----------------------------------------------------------

        [StringLength(100)]
        public string ChiefName { get; set; }

        [StringLength(100)]
        public string ChiefTitle { get; set; }

        public string PhoneNo { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [StringLength(200)]
        public string StreetAddress { get; set; }

        //------------------------------------------------------
        public List<Trainee>? Trainees { get; set; }
        //------------------------------------------------------

    }
}
