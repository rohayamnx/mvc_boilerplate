using System.ComponentModel.DataAnnotations;

namespace mvc_boilerplate.Models
{
    public class Module
    {
        [Key]
        public int ModuleId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ModuleName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    }
}