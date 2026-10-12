using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvc_boilerplate.Data;
using mvc_boilerplate.Models;
using mvc_boilerplate.Models.Dto;
using mvc_boilerplate.Models.UI;
using mvc_boilerplate.Models.ViewModels;
using mvc_boilerplate.Services;

namespace mvc_boilerplate.Controllers
{
    [Authorize]
    public class UserAccountController : BaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly IStaffHubApiService _staffHubApi;
        private readonly PasswordHasher<UserAccount> _passwordHasher = new();

        public UserAccountController(ApplicationDbContext db, IStaffHubApiService staffHubApi)
        {
            _db = db;
            _staffHubApi = staffHubApi;
        }

        // GET: UserAccount
        [Authorize(Policy = "UserAccount.View")]
        public async Task<IActionResult> Index(string username, string status, int page = 1, int pageSize = 10)
        {
            ViewData["Module"] = "UserAccount";
            ViewData["CurrentFilter"] = username;
            ViewData["StatusFilter"] = status;

            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var users = _db.UserAccounts
                .Include(u => u.Role)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(username))
            {
                users = users.Where(u => u.UserName.Contains(username));
            }

            if (!string.IsNullOrEmpty(status))
            {
                users = users.Where(u => u.Status == status);
            }

            // Create paginated list
            var paginatedUsers = await PaginatedList<UserAccount>.CreateAsync(users.OrderBy(u => u.UserName), page, pageSize);

            // Set up pagination info for the view component
            var routeValues = new Dictionary<string, string> { { "pageSize", pageSize.ToString() } };
            if (!string.IsNullOrEmpty(username))
            {
                routeValues.Add("username", username);
            }
            if (!string.IsNullOrEmpty(status))
            {
                routeValues.Add("status", status);
            }

            ViewBag.PaginationInfo = new PaginationInfo
            {
                CurrentPage = paginatedUsers.PageIndex,
                TotalPages = paginatedUsers.TotalPages,
                TotalItems = paginatedUsers.TotalCount,
                PageSize = paginatedUsers.PageSize,
                HasPrevious = paginatedUsers.HasPreviousPage,
                HasNext = paginatedUsers.HasNextPage,
                ActionName = "Index",
                ControllerName = "UserAccount",
                RouteValues = routeValues
            };

            return View(paginatedUsers);
        }

        // GET: UserAccount/Details/ADM001
        [Authorize(Policy = "UserAccount.View")]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _db.UserAccounts
                .Include(u => u.Role!)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission!)
                            .ThenInclude(p => p.Module!)
                .FirstOrDefaultAsync(u => u.UserAccountId == id);

            if (user is null)
                return NotFound();

            // Fetch staff information from StaffHub API (same pattern as iform_hra)
            StaffDto? staffInfo = null;
            try
            {
                if (!string.IsNullOrEmpty(user.UserAccountId))
                {
                    staffInfo = await _staffHubApi.GetStaffByIdAsync(user.UserAccountId);
                    if (staffInfo == null)
                    {
                        ViewData["ErrorMessage"] = $"Staff record not found for ID: {user.UserAccountId}. Please contact administrator.";
                    }
                }
            }
            catch
            {
                ViewData["ErrorMessage"] = "Unable to load staff information from StaffHub API. Please try again later.";
            }

            ViewBag.StaffInfo = staffInfo;

            return View(user);
        }

        // GET: UserAccount/Create
        [Authorize(Policy = "UserAccount.Create")]
        public async Task<IActionResult> Create()
        {
            ViewData["Module"] = "UserAccount";

            ViewBag.Roles = await _db.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.RoleName)
                .ToListAsync();

            return View(new CreateUserViewModel());
        }

        // POST: UserAccount/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "UserAccount.Create")]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            ViewData["Module"] = "UserAccount";

            ViewBag.Roles = await _db.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.RoleName)
                .ToListAsync();

            if (!ModelState.IsValid)
                return View(model);

            // Check for duplicate UserAccountId
            var exists = await _db.UserAccounts
                .AnyAsync(u => u.UserAccountId == model.UserAccountId);

            if (exists)
            {
                ModelState.AddModelError(nameof(model.UserAccountId), "This User ID is already in use.");
                return View(model);
            }

            // Validate RoleId exists
            var roleExists = await _db.Roles.AnyAsync(r => r.RoleId == model.RoleId);
            if (!roleExists)
            {
                ModelState.AddModelError(nameof(model.RoleId), "Selected role is invalid.");
                return View(model);
            }

            var user = new UserAccount
            {
                UserAccountId = model.UserAccountId,
                UserName = model.UserName,
                Email = model.Email,
                RoleId = model.RoleId,
                Status = model.Status,
                LoginProvider = "Local",
                PasswordHash = _passwordHasher.HashPassword(null!, model.Password),
                CreatedBy = User.FindFirst("UserAccountId")?.Value,
                CreatedAt = DateTime.Now
            };

            _db.UserAccounts.Add(user);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "User account created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: UserAccount/Edit/ADM001
        [Authorize(Policy = "UserAccount.Edit")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _db.UserAccounts.FindAsync(id);
            if (user is null)
                return NotFound();

            ViewData["Module"] = "UserAccount";
            ViewBag.Roles = await _db.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.RoleName)
                .ToListAsync();

            var model = new EditUserViewModel
            {
                UserAccountId = user.UserAccountId,
                UserName = user.UserName,
                Email = user.Email,
                RoleId = user.RoleId,
                Status = user.Status,
            };

            return View(model);
        }

        // POST: UserAccount/Edit/ADM001
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "UserAccount.Edit")]
        public async Task<IActionResult> Edit(string id, EditUserViewModel model)
        {
            if (id != model.UserAccountId)
                return NotFound();

            ViewData["Module"] = "UserAccount";
            ViewBag.Roles = await _db.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.RoleName)
                .ToListAsync();

            if (!string.IsNullOrEmpty(model.NewPassword) != !string.IsNullOrEmpty(model.ConfirmNewPassword))
                ModelState.AddModelError(string.Empty, "Please fill in both password fields.");

            if (!ModelState.IsValid)
                return View(model);

            var user = await _db.UserAccounts.FindAsync(id);
            if (user is null)
                return NotFound();

            var roleExists = await _db.Roles.AnyAsync(r => r.RoleId == model.RoleId);
            if (!roleExists)
            {
                ModelState.AddModelError(nameof(model.RoleId), "Selected role is invalid.");
                return View(model);
            }

            user.UserName = model.UserName;
            user.Email = model.Email;
            user.RoleId = model.RoleId;
            user.Status = model.Status;
            user.UpdatedBy = User.FindFirst("UserAccountId")?.Value;
            user.UpdatedAt = DateTime.Now;

            if (!string.IsNullOrEmpty(model.NewPassword))
                user.PasswordHash = _passwordHasher.HashPassword(null!, model.NewPassword);

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "User account updated successfully.";
            return RedirectToAction(nameof(Edit), new { id = id });
        }

        // API endpoint for modal - returns JSON
        [Authorize(Policy = "UserAccount.View")]
        [HttpGet]
        public async Task<IActionResult> GetRolesForModal(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "User ID is required" });

            var user = await _db.UserAccounts
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserAccountId == id);

            if (user == null)
                return Json(new { success = false, message = "User not found" });

            var currentRoleName = user.Role?.RoleName;

            var allRoles = await _db.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.RoleName)
                .ToListAsync();

            var rolesData = allRoles.Select(role => new
            {
                id = role.RoleId.ToString(),
                name = role.RoleName,
                isSelected = role.RoleName == currentRoleName
            }).ToList();

            return Json(new
            {
                success = true,
                userId = user.UserAccountId,
                userName = user.UserName,
                currentRole = currentRoleName,
                roles = rolesData
            });
        }

        // API endpoint for updating role via modal - returns JSON
        [Authorize(Policy = "UserAccount.Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRoleFromModal([FromForm] string userId, [FromForm] string selectedRoleId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Json(new { success = false, message = "User ID is required" });

            var user = await _db.UserAccounts.FindAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            // If a role is selected, validate it exists
            if (!string.IsNullOrEmpty(selectedRoleId))
            {
                if (!int.TryParse(selectedRoleId, out var roleId))
                    return Json(new { success = false, message = "Invalid role selected" });

                var roleExists = await _db.Roles.AnyAsync(r => r.RoleId == roleId && r.IsActive);
                if (!roleExists)
                    return Json(new { success = false, message = "Selected role is invalid or inactive" });

                user.RoleId = roleId;
            }

            user.UpdatedBy = User.FindFirst("UserAccountId")?.Value;
            user.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Role updated successfully" });
        }
    }
}