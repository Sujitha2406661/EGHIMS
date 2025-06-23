// Models/Organization.cs
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Health_Insurance.Models
{
    public class Organization
    {
        [Key]
        public int OrganizationId { get; set; }

        [Required(ErrorMessage = "Organization Name is required.")]
        [StringLength(100)]
        public string OrganizationName { get; set; }

        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Contact person's name can only contain alphabets and spaces.")]
        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; }

        // --- REMOVED PhoneNumber as it's not in your database ---
        //[StringLength(15)]
        //[RegularExpression(@"^[0-9]+$", ErrorMessage = "Phone number can only contain numbers.")]
        //[Display(Name = "Phone Number")]
        //public string PhoneNumber { get; set; }

        // --- RENAMED EmailAddress to ContactEmail to match your database ---
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [Display(Name = "Contact Email")] // Updated Display Name
        public string ContactEmail { get; set; } // Renamed from EmailAddress

        // Navigation property for employees in this organization
        [ValidateNever]
        public virtual ICollection<Employee> Employees { get; set; }
    }
}