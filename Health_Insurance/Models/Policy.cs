// Models/Policy.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Health_Insurance.Models
{
    // Represents a Policy entity, mapping to the Policy table in the database.
    public class Policy
    {
        [Key] // Specifies this property is the primary key
        public int PolicyId { get; set; }

        [Required]
        [StringLength(100)]
        public string PolicyName { get; set; }

        [Required]
        [Column(TypeName = "decimal(10, 2)")] // Specify SQL Server data type for precision
        [Range(0.01, double.MaxValue, ErrorMessage = "Premium Amount must be a positive number.")] // ADDED VALIDATION
        public decimal CoverageAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(10, 2)")] // Specify SQL Server data type for precision
        [Range(0.01, double.MaxValue, ErrorMessage = "Coverage Amount must be a positive number.")] // ADDED VALIDATION
        public decimal PremiumAmount { get; set; }

        // Policy Type as described in the document (ENUM in MySQL, using string with validation here)
        [Required]
        [StringLength(20)]
        // You might add validation here or in a Service layer to restrict values to 'INDIVIDUAL', 'FAMILY'
        public string PolicyType { get; set; }
    }
}

