using System.ComponentModel.DataAnnotations;

namespace Store_Management.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Employee ID is required.")]
        [StringLength(50, ErrorMessage = "Employee ID cannot exceed 50 characters.")]
        [Display(Name = "Employee ID")]
        public string EmployeeId { get; set; } = string.Empty;


        [Required(ErrorMessage = "New password is required.")]
        [StringLength(
            100,
            MinimumLength = 4,
            ErrorMessage = "Password must be at least 4 characters."
        )]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare(
            "NewPassword",
            ErrorMessage = "New Password and Confirm Password do not match."
        )]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}