using System.ComponentModel.DataAnnotations;

namespace Store_Management.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Employee ID is required.")]
        [StringLength(50, ErrorMessage = "Employee ID cannot exceed 50 characters.")]
        [Display(Name = "Employee ID")]
        public string EmployeeId { get; set; } = string.Empty;


        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please select a role.")]
        [Display(Name = "Role")]
        public string Role { get; set; } = string.Empty;


        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 4,
            ErrorMessage = "Password must be at least 4 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("Password",
            ErrorMessage = "Password and Confirm Password do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}