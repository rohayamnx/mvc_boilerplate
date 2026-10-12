using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvc_boilerplate.Data;
using mvc_boilerplate.Models;
using mvc_boilerplate.Models.Dto;
using mvc_boilerplate.Models.UI;

namespace mvc_boilerplate.Controllers
{
    /// <summary>
    /// Role &amp; permission management endpoints for the single-page
    /// "Role &amp; Permission Management" admin screen. Roles, Modules,
    /// Permissions and the RolePermission junction are all managed here.
    ///
    /// Unlike the eClinic backend (which stores permissions as Identity
    /// role claims), this project persists role↔permission assignments
    /// as rows in the <see cref="RolePermission"/> junction table.
    /// </summary>
    [Authorize]
    public class RolePermissionController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<RolePermissionController> _logger;

        public RolePermissionController(ApplicationDbContext db, ILogger<RolePermissionController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: /RolePermission
        [Authorize(Policy = "RolePermission.View")]
        public async Task<IActionResult> Index(string search, string status, int page = 1, int pageSize = 10)
        {
            ViewData["Module"] = "RolePermission";
            ViewData["CurrentFilter"] = search;
            ViewData["StatusFilter"] = status;

            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var query = _db.Roles.AsNoTracking().AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.RoleName.Contains(search)
                                      || (r.Description != null && r.Description.Contains(search)));
            }

            if (!string.IsNullOrEmpty(status))
            {
                bool? active = status == "ACTIVE" ? true
                             : status == "INACTIVE" ? false
                             : (bool?)null;
                if (active.HasValue) query = query.Where(r => r.IsActive == active.Value);
            }

            // Project to DTO server-side; the navigation-collection counts
            // translate to correlated subqueries (single round-trip).
            var projected = query.OrderBy(r => r.RoleName).Select(r => new RoleListDto
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                Description = r.Description,
                IsActive = r.IsActive,
                PermissionCount = r.RolePermissions.Count,
                UserCount = r.Users.Count,
            });

            // Create paginated list
            var paginatedRoles = await PaginatedList<RoleListDto>.CreateAsync(projected, page, pageSize);

            // Set up pagination info for the view component
            var routeValues = new Dictionary<string, string> { { "pageSize", pageSize.ToString() } };
            if (!string.IsNullOrEmpty(search))
            {
                routeValues.Add("search", search);
            }
            if (!string.IsNullOrEmpty(status))
            {
                routeValues.Add("status", status);
            }

            ViewBag.PaginationInfo = new PaginationInfo
            {
                CurrentPage = paginatedRoles.PageIndex,
                TotalPages = paginatedRoles.TotalPages,
                TotalItems = paginatedRoles.TotalCount,
                PageSize = paginatedRoles.PageSize,
                HasPrevious = paginatedRoles.HasPreviousPage,
                HasNext = paginatedRoles.HasNextPage,
                ActionName = "Index",
                ControllerName = "RolePermission",
                RouteValues = routeValues,
            };

            return View(paginatedRoles);
        }

        // ── Role list ──────────────────────────────────────────────────────

        /// <summary>Fetch one role with its module/permission groups and
        /// IsSelected flags reflecting current RolePermission rows.</summary>
        [HttpGet]
        [Authorize(Policy = "RolePermission.View")]
        public async Task<IActionResult> Detail(int id)
        {
            var role = await _db.Roles.FindAsync(id);
            if (role is null)
                return NotFound(new { message = $"Role '{id}' not found." });

            var selectedPermIds = await _db.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == id)
                .Select(rp => rp.PermissionId)
                .ToHashSetAsync();

            // Project with .Select() so EF emits an efficient correlated
            // subquery instead of a collection .Include() JOIN (avoids
            // Cartesian explosion). IsSelected is applied in memory.
            var groups = await _db.Modules
                .AsNoTracking()
                .OrderBy(m => m.ModuleName)
                .Select(m => new ModulePermissionGroupDto
                {
                    ModuleId = m.ModuleId,
                    ModuleName = m.ModuleName,
                    Description = m.Description,
                    IsActive = m.IsActive,
                    Permissions = m.Permissions
                        .OrderBy(p => p.PermissionName)
                        .Select(p => new PermissionItemDto
                        {
                            PermissionId = p.PermissionId,
                            PermissionName = p.PermissionName,
                            Description = p.Description,
                            IsActive = p.IsActive,
                            ClaimValue = m.ModuleName + "." + p.PermissionName,
                            IsSelected = false,
                        })
                        .ToList(),
                })
                .ToListAsync();

            foreach (var group in groups)
                foreach (var perm in group.Permissions)
                    perm.IsSelected = selectedPermIds.Contains(perm.PermissionId);

            return Json(new RoleDetailDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description,
                IsActive = role.IsActive,
                PermissionCount = selectedPermIds.Count,
                UserCount = await _db.UserAccounts.CountAsync(u => u.RoleId == id),
                ModuleGroups = groups,
                CreatedBy = role.CreatedBy,
                CreatedAt = role.CreatedAt,
                UpdatedBy = role.UpdatedBy,
                UpdatedAt = role.UpdatedAt,
            });
        }

        /// <summary>Create a new role.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RolePermission.Create")]
        public async Task<IActionResult> Create([FromBody] RoleCreateDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.RoleName))
                return BadRequest(new { message = "Role name is required." });

            var name = dto.RoleName.Trim();
            if (await _db.Roles.AnyAsync(r => r.RoleName == name))
                return BadRequest(new { message = $"Role '{name}' already exists." });

            var role = new Role
            {
                RoleName = name,
                Description = dto.Description,
                IsActive = dto.IsActive,
                CreatedBy = CurrentUser(),
                CreatedAt = DateTime.Now,
            };

            _db.Roles.Add(role);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Created role {RoleId} ({RoleName})", role.RoleId, role.RoleName);

            return Json(new RoleListDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description,
                IsActive = role.IsActive,
                PermissionCount = 0,
                UserCount = 0,
            });
        }

        /// <summary>Rename / update a role.</summary>
        [HttpPut]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RolePermission.Edit")]
        public async Task<IActionResult> Update(int id, [FromBody] RoleUpdateDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.RoleName))
                return BadRequest(new { message = "Role name is required." });

            var role = await _db.Roles.FindAsync(id);
            if (role is null)
                return NotFound(new { message = $"Role '{id}' not found." });

            var name = dto.RoleName.Trim();
            if (!string.Equals(role.RoleName, name, StringComparison.OrdinalIgnoreCase)
                && await _db.Roles.AnyAsync(r => r.RoleName == name))
            {
                return BadRequest(new { message = $"Role '{name}' already exists." });
            }

            role.RoleName = name;
            role.Description = dto.Description;
            role.IsActive = dto.IsActive;
            role.UpdatedBy = CurrentUser();
            role.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            _logger.LogInformation("Updated role {RoleId} -> {RoleName}", id, role.RoleName);

            return Json(new
            {
                roleId = role.RoleId,
                roleName = role.RoleName,
                description = role.Description,
                isActive = role.IsActive,
            });
        }

        /// <summary>Delete a role. Refuses if users are still assigned.</summary>
        [HttpDelete]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RolePermission.Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _db.Roles.FindAsync(id);
            if (role is null)
                return NotFound(new { message = $"Role '{id}' not found." });

            var userCount = await _db.UserAccounts.CountAsync(u => u.RoleId == id);
            if (userCount > 0)
                return BadRequest(new { message = $"Cannot delete role '{role.RoleName}' because {userCount} user(s) are still assigned. Remove them first." });

            // Cascade removes RolePermission rows (configured in DbContext).
            _db.Roles.Remove(role);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Deleted role {RoleId} ({RoleName})", id, role.RoleName);

            return NoContent();
        }

        // ── Role ↔ Permission assignment (RolePermission junction) ─────────

        /// <summary>Replace a role's assigned permissions. Diffs against the
        /// existing RolePermission rows: keeps unchanged rows (preserving
        /// audit), adds newly selected, removes unselected.</summary>
        [HttpPut]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RolePermission.Edit")]
        public async Task<IActionResult> UpdatePermissions(int id, [FromBody] UpdateRolePermissionsDto dto)
        {
            var role = await _db.Roles.FindAsync(id);
            if (role is null)
                return NotFound(new { message = $"Role '{id}' not found." });

            dto ??= new UpdateRolePermissionsDto();

            var requested = dto.PermissionIds.Distinct().ToHashSet();
            // Validate the submitted permission ids exist.
            var validIds = await _db.Permissions
                .AsNoTracking()
                .Where(p => requested.Contains(p.PermissionId))
                .Select(p => p.PermissionId)
                .ToHashSetAsync();

            var existing = await _db.RolePermissions
                .Where(rp => rp.RoleId == id)
                .ToListAsync();

            var toRemove = existing.Where(e => !validIds.Contains(e.PermissionId)).ToList();
            var toAdd = validIds
                .Where(pid => !existing.Any(e => e.PermissionId == pid))
                .Select(pid => new RolePermission
                {
                    RoleId = id,
                    PermissionId = pid,
                    CreatedBy = CurrentUser(),
                    CreatedAt = DateTime.Now,
                })
                .ToList();

            _db.RolePermissions.RemoveRange(toRemove);
            _db.RolePermissions.AddRange(toAdd);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Updated permissions for role {RoleId} ({RoleName}): {Count} permission(s)",
                id, role.RoleName, validIds.Count);

            return Json(new { message = $"Permissions updated for '{role.RoleName}'.", count = validIds.Count });
        }

        // ── Module &amp; Permission catalog ───────────────────────────────

        /// <summary>List all modules with their permissions (catalog grid).</summary>
        [HttpGet]
        [Authorize(Policy = "RolePermission.View")]
        public async Task<IActionResult> Modules()
        {
            var groups = await _db.Modules
                .AsNoTracking()
                .OrderBy(m => m.ModuleName)
                .Select(m => new ModulePermissionGroupDto
                {
                    ModuleId = m.ModuleId,
                    ModuleName = m.ModuleName,
                    Description = m.Description,
                    IsActive = m.IsActive,
                    Permissions = m.Permissions
                        .OrderBy(p => p.PermissionName)
                        .Select(p => new PermissionItemDto
                        {
                            PermissionId = p.PermissionId,
                            PermissionName = p.PermissionName,
                            Description = p.Description,
                            IsActive = p.IsActive,
                            ClaimValue = m.ModuleName + "." + p.PermissionName,
                        })
                        .ToList(),
                })
                .ToListAsync();

            return Json(groups);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RolePermission.Create")]
        public async Task<IActionResult> CreateModule([FromBody] ModuleCreateDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.ModuleName))
                return BadRequest(new { message = "Module name is required." });

            var name = dto.ModuleName.Trim();
            if (await _db.Modules.AnyAsync(m => m.ModuleName == name))
                return BadRequest(new { message = $"Module '{name}' already exists." });

            var module = new Module
            {
                ModuleName = name,
                Description = dto.Description,
                IsActive = true,
                CreatedBy = CurrentUser(),
                CreatedAt = DateTime.Now,
            };

            _db.Modules.Add(module);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Created module {ModuleId} ({ModuleName})", module.ModuleId, module.ModuleName);

            return Json(new ModulePermissionGroupDto
            {
                ModuleId = module.ModuleId,
                ModuleName = module.ModuleName,
                Description = module.Description,
                IsActive = module.IsActive,
                Permissions = new List<PermissionItemDto>(),
            });
        }

        /// <summary>Rename / update a module. Refuses if it has permissions
        /// and the caller is trying to deactivate it (keep catalog consistent).</summary>
        [HttpPut]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RolePermission.Edit")]
        public async Task<IActionResult> UpdateModule(int id, [FromBody] ModuleUpdateDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.ModuleName))
                return BadRequest(new { message = "Module name is required." });

            var module = await _db.Modules.FindAsync(id);
            if (module is null)
                return NotFound(new { message = $"Module '{id}' not found." });

            var name = dto.ModuleName.Trim();
            if (!string.Equals(module.ModuleName, name, StringComparison.OrdinalIgnoreCase)
                && await _db.Modules.AnyAsync(m => m.ModuleName == name))
            {
                return BadRequest(new { message = $"Module '{name}' already exists." });
            }

            module.ModuleName = name;
            module.Description = dto.Description;
            module.IsActive = dto.IsActive;
            module.UpdatedBy = CurrentUser();
            module.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            _logger.LogInformation("Updated module {ModuleId} -> {ModuleName}", id, module.ModuleName);

            return Json(new { moduleId = module.ModuleId, moduleName = module.ModuleName, description = module.Description, isActive = module.IsActive });
        }

        /// <summary>Delete a module. Refuses if it still has permissions.</summary>
        [HttpDelete]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RolePermission.Delete")]
        public async Task<IActionResult> DeleteModule(int id)
        {
            var module = await _db.Modules
                .Include(m => m.Permissions)
                .FirstOrDefaultAsync(m => m.ModuleId == id);
            if (module is null)
                return NotFound(new { message = $"Module '{id}' not found." });

            if (module.Permissions.Count > 0)
                return BadRequest(new { message = $"Cannot delete module '{module.ModuleName}' because it has {module.Permissions.Count} permission(s). Delete the permissions first." });

            _db.Modules.Remove(module);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Deleted module {ModuleId} ({ModuleName})", id, module.ModuleName);

            return NoContent();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RolePermission.Create")]
        public async Task<IActionResult> CreatePermission([FromBody] PermissionCreateDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.PermissionName))
                return BadRequest(new { message = "Permission name is required." });

            var module = await _db.Modules.FindAsync(dto.ModuleId);
            if (module is null)
                return BadRequest(new { message = $"Module '{dto.ModuleId}' not found." });

            var name = dto.PermissionName.Trim();
            if (await _db.Permissions.AnyAsync(p => p.ModuleId == dto.ModuleId
                && p.PermissionName.ToLower() == name.ToLower()))
            {
                return BadRequest(new { message = $"Permission '{name}' already exists under module '{module.ModuleName}'." });
            }

            var permission = new Permission
            {
                ModuleId = dto.ModuleId,
                PermissionName = name,
                Description = dto.Description,
                IsActive = true,
                CreatedBy = CurrentUser(),
                CreatedAt = DateTime.Now,
            };

            _db.Permissions.Add(permission);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Created permission {PermissionId} ({Module}.{Permission})",
                permission.PermissionId, module.ModuleName, permission.PermissionName);

            return Json(new PermissionItemDto
            {
                PermissionId = permission.PermissionId,
                PermissionName = permission.PermissionName,
                Description = permission.Description,
                IsActive = permission.IsActive,
                ClaimValue = $"{module.ModuleName}.{permission.PermissionName}",
            });
        }

        /// <summary>Rename / update a permission.</summary>
        [HttpPut]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RolePermission.Edit")]
        public async Task<IActionResult> UpdatePermission(int id, [FromBody] PermissionUpdateDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.PermissionName))
                return BadRequest(new { message = "Permission name is required." });

            var permission = await _db.Permissions
                .Include(p => p.Module)
                .FirstOrDefaultAsync(p => p.PermissionId == id);
            if (permission is null)
                return NotFound(new { message = $"Permission '{id}' not found." });

            var name = dto.PermissionName.Trim();
            if (!string.Equals(permission.PermissionName, name, StringComparison.OrdinalIgnoreCase)
                && await _db.Permissions.AnyAsync(p => p.ModuleId == permission.ModuleId
                    && p.PermissionName.ToLower() == name.ToLower()))
            {
                return BadRequest(new { message = $"Permission '{name}' already exists under this module." });
            }

            permission.PermissionName = name;
            permission.Description = dto.Description;
            permission.IsActive = dto.IsActive;
            permission.UpdatedBy = CurrentUser();
            permission.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            _logger.LogInformation("Updated permission {PermissionId} -> {PermissionName}", id, permission.PermissionName);

            return Json(new
            {
                permissionId = permission.PermissionId,
                permissionName = permission.PermissionName,
                description = permission.Description,
                isActive = permission.IsActive,
                claimValue = $"{permission.Module?.ModuleName}.{permission.PermissionName}",
            });
        }

        /// <summary>Delete a permission. Refuses if any role currently has it
        /// assigned (via RolePermission).</summary>
        [HttpDelete]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RolePermission.Delete")]
        public async Task<IActionResult> DeletePermission(int id)
        {
            var permission = await _db.Permissions
                .Include(p => p.Module)
                .FirstOrDefaultAsync(p => p.PermissionId == id);
            if (permission is null)
                return NotFound(new { message = $"Permission '{id}' not found." });

            var assignedCount = await _db.RolePermissions.CountAsync(rp => rp.PermissionId == id);
            if (assignedCount > 0)
                return BadRequest(new { message = $"Cannot delete permission '{permission.Module?.ModuleName}.{permission.PermissionName}' because it is assigned to {assignedCount} role(s). Remove it from those roles first." });

            _db.Permissions.Remove(permission);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Deleted permission {PermissionId}", id);

            return NoContent();
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private string CurrentUser() => User.FindFirst("UserAccountId")?.Value ?? "System";
    }
}