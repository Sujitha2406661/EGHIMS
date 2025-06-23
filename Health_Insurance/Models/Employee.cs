// Models/Employee.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace Health_Insurance.Models
{
    // Represents an Employee entity, mapping to the Employee table in the database.
    public class Employee
    {
        [Key] // Specifies this property is the primary key
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Name can only contain alphabets and spaces.")] // NEW VALIDATION

        public string Name { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(15)]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Phone number can only contain numbers.")] // NEW VALIDATION

        public string Phone { get; set; }

        public string Address { get; set; }

        [StringLength(50)]
        public string Designation { get; set; }

        // Foreign Key to the Organization table
        [Required(ErrorMessage = "Organization is required.")]
        public int OrganizationId { get; set; }

        // Navigation property to the related Organization
        [ForeignKey("OrganizationId")] // Specifies the foreign key property
        [BindNever] // ADD THIS ATTRIBUTE! Tells model binder to ignore this property from form data
        public virtual Organization? Organization { get; set; }

        // --- Authentication Fields for Employee Login ---
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50)]
        public string Username { get; set; }

        // Password hash. Not [Required] on the model, but handled for initial creation in controller.
        [StringLength(256)]
        public string PasswordHash { get; set; }

        [Required(ErrorMessage = "Create Date is required.")]
        [Display(Name = "Create Date")]
        public DateTime CreateDate { get; set; } = DateTime.Now;
    }
}


