namespace mvc_boilerplate.Models.Dto
{
    // ── List / detail shapes ───────────────────────────────────────────────

    /// <summary>A role row in the management table.</summary>
    public class RoleListDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int PermissionCount { get; set; }
        public int UserCount { get; set; }
    }

    /// <summary>Full role detail with module/permission groups (IsSelected
    /// reflects the role's current RolePermission rows).</summary>
    public class RoleDetailDto : RoleListDto
    {
        public List<ModulePermissionGroupDto> ModuleGroups { get; set; } = new();
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>A module with its permissions, for the catalog and the
    /// permission checkbox grid.</summary>
    public class ModulePermissionGroupDto
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public List<PermissionItemDto> Permissions { get; set; } = new();
    }

    /// <summary>A single permission within a module group.</summary>
    public class PermissionItemDto
    {
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        /// <summary>Display hint: "ModuleName.PermissionName".</summary>
        public string ClaimValue { get; set; } = string.Empty;
        /// <summary>Only populated from Detail endpoint.</summary>
        public bool IsSelected { get; set; }
    }

    // ── Payloads ───────────────────────────────────────────────────────────

    public class RoleCreateDto
    {
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class RoleUpdateDto
    {
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateRolePermissionsDto
    {
        public List<int> PermissionIds { get; set; } = new();
    }

    public class ModuleCreateDto
    {
        public string ModuleName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class ModuleUpdateDto
    {
        public string ModuleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class PermissionCreateDto
    {
        public int ModuleId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class PermissionUpdateDto
    {
        public string PermissionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}