using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mvc_boilerplate.Models
{
    // ISUZU Employee No; also used as login ID
    public class UserAccount
    {
        [Key]
        [MaxLength(20)]
        public string UserAccountId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string UserName { get; set; } = string.Empty;

        [MaxLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [MaxLength(500)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [ForeignKey(nameof(Role))]
        public int RoleId { get; set; }

        public virtual Role? Role { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "ACTIVE";

        [Required]
        [MaxLength(50)]
        public string LoginProvider { get; set; } = string.Empty;

        public DateTime? LastLoginAt { get; set; }

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
