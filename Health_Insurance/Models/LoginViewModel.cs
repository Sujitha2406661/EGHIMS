// Models/LoginViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace Health_Insurance.Models
{
    // ViewModel for handling login form data
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)] 
        [StringLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
        public string Password { get; set; } 

        // New property to indicate the type of login attempt (e.g., "Admin", "Employee")
        [Required(ErrorMessage = "Login type is required.")]
        public string LoginType { get; set; }
    }
}


