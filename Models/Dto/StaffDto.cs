namespace mvc_boilerplate.Models.Dto
{
    /// <summary>
    /// Data Transfer Object for Staff response from StaffHub API
    /// </summary>
    public class StaffDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ContactNo { get; set; }
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string DesignationId { get; set; } = string.Empty;
        public string DesignationName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime DateJoined { get; set; }
        public string? SectionId { get; set; }
        public string? SectionName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}