using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using mvc_boilerplate.Models;

namespace mvc_boilerplate.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Apply any pending migrations
            await db.Database.MigrateAsync();

            // 1. Seed Modules
            var moduleDefinitions = new (string Name, string? Description)[]
            {
                ("UserAccount", "User account management"),
                ("RolePermission", "Role & permission management"),
            };

            var moduleMap = new Dictionary<string, Module>();
            foreach (var (name, desc) in moduleDefinitions)
            {
                var module = await db.Modules.FirstOrDefaultAsync(m => m.ModuleName == name);
                if (module == null)
                {
                    module = new Module
                    {
                        ModuleName = name,
                        Description = desc,
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedAt = DateTime.Now
                    };
                    db.Modules.Add(module);
                    await db.SaveChangesAsync();
                }
                moduleMap[name] = module;
            }

            // 2. Seed Permissions (View, Create, Edit, Delete for each module)
            var permissionActions = new[] { "View", "Create", "Edit", "Delete" };
            var permissionMap = new Dictionary<string, Permission>(); // key: "Module.Action"

            foreach (var (moduleName, _) in moduleDefinitions)
            {
                var moduleId = moduleMap[moduleName].ModuleId;
                foreach (var action in permissionActions)
                {
                    var permName = action;
                    var permission = await db.Permissions.FirstOrDefaultAsync(
                        p => p.ModuleId == moduleId && p.PermissionName == permName);

                    if (permission == null)
                    {
                        permission = new Permission
                        {
                            ModuleId = moduleId,
                            PermissionName = permName,
                            Description = $"{action} access to {moduleName}",
                            IsActive = true,
                            CreatedBy = "System",
                            CreatedAt = DateTime.Now
                        };
                        db.Permissions.Add(permission);
                        await db.SaveChangesAsync();
                    }
                    permissionMap[$"{moduleName}.{action}"] = permission;
                }
            }

            // 3. Seed Roles
            if (!await db.Roles.AnyAsync(r => r.RoleName == "SuperAdmin"))
            {
                db.Roles.Add(new Role
                {
                    RoleName = "SuperAdmin",
                    Description = "Full system access",
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedAt = DateTime.Now
                });
                await db.SaveChangesAsync();
            }

            var superAdminRole = await db.Roles.FirstAsync(r => r.RoleName == "SuperAdmin");

            // 4. Seed RolePermissions — SuperAdmin gets every permission
            foreach (var perm in permissionMap.Values)
            {
                var exists = await db.RolePermissions.AnyAsync(
                    rp => rp.RoleId == superAdminRole.RoleId && rp.PermissionId == perm.PermissionId);

                if (!exists)
                {
                    db.RolePermissions.Add(new RolePermission
                    {
                        RoleId = superAdminRole.RoleId,
                        PermissionId = perm.PermissionId,
                        CreatedBy = "System",
                        CreatedAt = DateTime.Now
                    });
                }
            }
            await db.SaveChangesAsync();

            // 5. Seed default SuperAdmin user
            if (!await db.UserAccounts.AnyAsync(u => u.UserAccountId == "ADM001"))
            {
                var hasher = new PasswordHasher<UserAccount>();
                var user = new UserAccount
                {
                    UserAccountId = "ADM001",
                    UserName = "System Admin",
                    RoleId = superAdminRole.RoleId,
                    Status = "ACTIVE",
                    CreatedBy = "System",
                    CreatedAt = DateTime.Now
                };

                // Default password: 1234Abcd#
                user.PasswordHash = hasher.HashPassword(user, "1234Abcd#");

                db.UserAccounts.Add(user);
                await db.SaveChangesAsync();
            }
        }
    }
}