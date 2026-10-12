using System.ComponentModel.DataAnnotations;

namespace mvc_boilerplate.Models.ViewModels
{
    public class CreateUserViewModel
    {
        [Required]
        [StringLength(20, ErrorMessage = "User ID cannot exceed 20 characters.")]
        [Display(Name = "User ID")]
        public string UserAccountId { get; set; } = string.Empty;

        [Required]
        [StringLength(200, ErrorMessage = "Username cannot exceed 200 characters.")]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [MaxLength(100)]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required]
        [StringLength(128, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [StrongPassword]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role")]
        public int RoleId { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "ACTIVE";
    }
}