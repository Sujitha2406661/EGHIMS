// Models/Claim.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Health_Insurance.Models
{
    public class Claim
    {
        [Key]
        public int ClaimId { get; set; }

        [Required(ErrorMessage = "Enrollment ID is required.")]
        public int EnrollmentId { get; set; }

        [ForeignKey("EnrollmentId")]
        [BindNever]
        public virtual Enrollment? Enrollment { get; set; }

        [Required(ErrorMessage = "Claim amount is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Claim amount must be greater than 0.")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal ClaimAmount { get; set; }

        [Required(ErrorMessage = "Claim date is required.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime ClaimDate { get; set; }
        [StringLength(500, ErrorMessage = "Claim reason cannot exceed 500 characters.")]
        public string ClaimReason { get; set; }

        [Required(ErrorMessage = "Claim status is required.")]
        [StringLength(50)]
        public string ClaimStatus { get; set; } = "SUBMITTED"; // Default status
    }
}




