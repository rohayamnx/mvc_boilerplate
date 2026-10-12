using System.ComponentModel.DataAnnotations;

namespace mvc_boilerplate.Models.ViewModels
{
    public class EditUserViewModel
    {
        [Required]
        [StringLength(20)]
        public string UserAccountId { get; set; } = string.Empty;

        [Required]
        [StringLength(200, ErrorMessage = "Username cannot exceed 200 characters.")]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [MaxLength(100)]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        // Optional on edit — only update if provided
        [StringLength(128, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        [StrongPassword]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmNewPassword { get; set; }

        [Required]
        [Display(Name = "Role")]
        public int RoleId { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "ACTIVE";
    }
}
