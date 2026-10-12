using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using mvc_boilerplate.Data;
using mvc_boilerplate.Models;
using mvc_boilerplate.Models.Dto;
using mvc_boilerplate.Models.Session;
using mvc_boilerplate.Models.UI;
using mvc_boilerplate.Services;
using mvc_boilerplate.Models.ViewModels;
using System.Security.Claims;

namespace mvc_boilerplate.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IStaffHubApiService _staffHubApi;
        private readonly IADAuthenticationService _adAuth;
        private readonly IStaffHubAuthInfoService _staffHubAuthInfo;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;
        private readonly PasswordHasher<UserAccount> _passwordHasher = new();

        public AccountController(
            ApplicationDbContext db,
            IStaffHubApiService staffHubApi,
            IADAuthenticationService adAuth,
            IStaffHubAuthInfoService staffHubAuthInfo,
            IConfiguration configuration,
            ILogger<AccountController> logger)
        {
            _db = db;
            _staffHubApi = staffHubApi;
            _adAuth = adAuth;
            _staffHubAuthInfo = staffHubAuthInfo;
            _configuration = configuration;
            _logger = logger;
        }

        // ─── Login (GET) ──────────────────────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            returnUrl ??= "/";

            // Check if Azure AD button should be displayed (via StaffHub auth info)
            try
            {
                var authInfo = await _staffHubAuthInfo.GetAuthInfoAsync();
                if (authInfo != null && !string.IsNullOrEmpty(authInfo.AzureRedirectUrl))
                {
                    ViewBag.AzureRedirectUrl = Url.Action("AzureSignIn", "Account", new { returnUrl });
                }
            }
            catch
            {
                // StaffHub unavailable — Azure AD button won't show
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // ─── Login (POST) — Local AD first, then Local DB ─────────────

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var staffId = model.Email;
            if (staffId.Contains("@"))
            {
                staffId = staffId.Split("@")[0];
            }

            // Step 1: Try Local AD Authentication
            var isValidAd = await _adAuth.ValidateCredentialsAsync(staffId, model.Password);
            if (isValidAd)
            {
                var adUser = await GetOrCreateLocalUserAsync(staffId, loginProvider: "ActiveDirectory");
                if (adUser != null)
                {
                    await SignInAndStoreSessionAsync(adUser, model.RememberMe);
                    return RedirectAfterLogin(model.ReturnUrl);
                }
            }

            // Step 2: Fall back to Local DB Authentication
            var localUser = await _db.UserAccounts
                .Include(u => u!.Role)
                .FirstOrDefaultAsync(u => u.UserAccountId == staffId);

            if (localUser is null || localUser.Status != "ACTIVE")
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            var passwordResult = _passwordHasher.VerifyHashedPassword(localUser, localUser.PasswordHash, model.Password);
            if (passwordResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            // Sync with StaffHub profile (same path as AD/Azure)
            var syncedUser = await GetOrCreateLocalUserAsync(staffId, loginProvider: "Local");
            if (syncedUser != null)
            {
                await SignInAndStoreSessionAsync(syncedUser, model.RememberMe);
                return RedirectAfterLogin(model.ReturnUrl);
            }

            await SignInAndStoreSessionAsync(localUser, model.RememberMe);
            return RedirectAfterLogin(model.ReturnUrl);
        }

        // ─── Azure AD Sign-in ─────────────────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> AzureSignIn(string? returnUrl = null)
        {
            try
            {
                var azureRedirectUrl = await GetAzureRedirectUrlAsync(returnUrl);
                if (!string.IsNullOrEmpty(azureRedirectUrl))
                {
                    return Redirect(azureRedirectUrl);
                }

                TempData["ErrorMessage"] = "Azure AD authentication is not available.";
                return RedirectToAction("Login", new { returnUrl });
            }
            catch
            {
                TempData["ErrorMessage"] = "An error occurred while initiating Azure AD authentication.";
                return RedirectToAction("Login", new { returnUrl });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> AzureCallback(string? auth_success = null, string? session_data = null, string? returnUrl = null)
        {
            if (auth_success != "true" || string.IsNullOrEmpty(session_data))
            {
                // _logger.LogWarning("AzureCallback: received without proper authentication data");
                TempData["ErrorMessage"] = "Authentication session expired or invalid.";
                return RedirectToAction("Login", new { returnUrl });
            }

            try
            {
                var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(session_data));
                var sessionResponse = JsonSerializer.Deserialize<SessionLoginResponse>(decoded, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (sessionResponse?.Profile == null)
                {
                    // _logger.LogWarning("AzureCallback: invalid session data received — Profile is null");
                    TempData["ErrorMessage"] = "Invalid authentication data received.";
                    return RedirectToAction("Login", new { returnUrl });
                }

                // Security (C-3 fix): Validate the SessionId with StaffHub before trusting
                // the identity in session_data. This call goes through StaffHub's
                // ApiClientValidationMiddleware which enforces AllowedOriginsJson from
                // the [ApiClients] table. A crafted session_data blob with a fake
                // StaffId but no real session will be rejected here.
                if (string.IsNullOrEmpty(sessionResponse.SessionId) ||
                    !await ValidateStaffHubSessionAsync(sessionResponse.SessionId))
                {
                    // _logger.LogWarning("AzureCallback: StaffHub session validation failed for SessionId {SessionId}",
                    //     sessionResponse.SessionId);
                    TempData["ErrorMessage"] = "Your session could not be validated. Please try logging in again.";
                    return RedirectToAction("Login", new { returnUrl });
                }

                // Extract StaffId from profile
                var staffId = !string.IsNullOrEmpty(sessionResponse.Profile.StaffId)
                    ? sessionResponse.Profile.StaffId
                    : sessionResponse.Profile.Email;

                if (!string.IsNullOrEmpty(staffId) && staffId.Contains("@"))
                {
                    staffId = staffId.Split("@")[0];
                }

                var user = await GetOrCreateLocalUserAsync(staffId, loginProvider: "Azure AD");
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Failed to create user account. Please contact administrator.";
                    return RedirectToAction("Login", new { returnUrl });
                }

                // _logger.LogInformation("Azure AD login successful — StaffId: {StaffId}, UserName: {UserName}",
                //     staffId, user.UserName);
                await SignInAndStoreSessionAsync(user, isPersistent: false);
                return RedirectAfterLogin(returnUrl ?? "/");
            }
            catch (Exception)
            {
                // _logger.LogError(ex, "AzureCallback: error processing session data");
                TempData["ErrorMessage"] = "Error processing authentication data. Please try again.";
                return RedirectToAction("Login", new { returnUrl });
            }
        }

        // ─── Logout ───────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.ClearUserSession();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // ─── Private helpers ──────────────────────────────────────────

        /// <summary>
        /// Gets or creates a local UserAccount for the given staffId.
        /// Profile data (UserName, Email) is resolved from StaffHub → AD → StaffId fallback.
        /// </summary>
        private async Task<UserAccount?> GetOrCreateLocalUserAsync(string staffId, string loginProvider)
        {
            // Resolve profile: StaffHub first, then AD fallback
            string? resolvedEmail = null;
            string resolvedName = staffId;

            try
            {
                var staff = await _staffHubApi.GetStaffByIdAsync(staffId);
                if (staff != null)
                {
                    resolvedName = !string.IsNullOrEmpty(staff.Name) ? staff.Name : staffId;
                    resolvedEmail = staff.Email;
                }
            }
            catch
            {
                // StaffHub unavailable — continue with AD/local
            }

            // AD email fallback
            if (string.IsNullOrEmpty(resolvedEmail))
            {
                try
                {
                    resolvedEmail = await _adAuth.GetAdUserEmailAsync(staffId);
                }
                catch
                {
                    // AD unavailable
                }
            }

            // Find existing local user
            var user = await _db.UserAccounts
                .Include(u => u!.Role)
                .FirstOrDefaultAsync(u => u.UserAccountId == staffId);

            if (user == null)
            {
                // Auto-create from AD/Azure — assign default role
                var defaultRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "User")
                    ?? await _db.Roles.FirstAsync();

                user = new UserAccount
                {
                    UserAccountId = staffId,
                    UserName = resolvedName,
                    Email = resolvedEmail,
                    RoleId = defaultRole.RoleId,
                    Status = "ACTIVE",
                    LoginProvider = loginProvider,
                    CreatedBy = "System",
                    CreatedAt = DateTime.Now
                };

                _db.UserAccounts.Add(user);
                await _db.SaveChangesAsync();
            }
            else
            {
                // Sync UserName and Email from StaffHub/AD if different
                bool needsUpdate = false;

                if (!string.IsNullOrEmpty(resolvedName) && user.UserName != resolvedName)
                {
                    user.UserName = resolvedName;
                    needsUpdate = true;
                }

                if (!string.IsNullOrEmpty(resolvedEmail) && user.Email != resolvedEmail)
                {
                    user.Email = resolvedEmail;
                    needsUpdate = true;
                }

                if (string.IsNullOrEmpty(user.LoginProvider))
                {
                    user.LoginProvider = loginProvider;
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    user.UpdatedAt = DateTime.Now;
                    user.UpdatedBy = staffId;
                    await _db.SaveChangesAsync();
                }
            }

            return user;
        }

        /// <summary>
        /// Signs in the user with claims and stores session data.
        /// Profile hierarchy: StaffHub → AD → Local DB
        /// </summary>
        private async Task SignInAndStoreSessionAsync(UserAccount user, bool isPersistent)
        {
            // Load this role's permission claims from the RolePermissions
            // junction table (RoleId + PermissionId) and project each to the
            // "{ModuleName}.{PermissionName}" claim value used across the app
            // (matches ClaimValue built in RolePermissionController). This is
            // the bridge between mvc_boilerplate's junction-table storage and
            // ckd_tracing-style User.HasClaim("Permission", ...) checks in views.
            var permissionClaimValues = await _db.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == user.RoleId && rp.Permission != null && rp.Permission.Module != null)
                .Select(rp => rp.Permission!.Module!.ModuleName + "." + rp.Permission!.PermissionName)
                .ToListAsync();

            var claims = new List<Claim>
            {
                new("UserAccountId", user.UserAccountId),
                new("UserName", user.UserName),
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.NameIdentifier, user.UserAccountId),
                new(ClaimTypes.Role, user.Role?.RoleName ?? string.Empty),
            };
            foreach (var claimValue in permissionClaimValues.Distinct())
            {
                claims.Add(new Claim("Permission", claimValue));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            user.LastLoginAt = DateTime.Now;
            await _db.SaveChangesAsync();

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = isPersistent });

            HttpContext.Session.SetString("UserAccountId", user.UserAccountId);

            // Store profile in session: StaffHub → AD → Local DB fallback
            await StoreUserSessionDataAsync(user.UserAccountId, user);
        }

        /// <summary>
        /// Fetches staff profile from StaffHub API and stores it in session.
        /// Falls back to local UserAccount data if StaffHub doesn't have the record.
        /// </summary>
        private async Task StoreUserSessionDataAsync(string staffId, UserAccount localUser)
        {
            try
            {
                var staff = await _staffHubApi.GetStaffByIdAsync(staffId);

                if (staff != null)
                {
                    HttpContext.Session.SetUserSession(new UserSessionData
                    {
                        StaffId = staff.Id,
                        Name = staff.Name,
                        Email = staff.Email,
                        ContactNo = staff.ContactNo,
                        DepartmentId = staff.DepartmentId,
                        DepartmentName = staff.DepartmentName,
                        DesignationId = staff.DesignationId,
                        DesignationName = staff.DesignationName,
                        SectionId = staff.SectionId,
                        SectionName = staff.SectionName,
                        Category = staff.Category,
                        DateJoined = staff.DateJoined,
                        IsActive = staff.IsActive,
                        SessionCreated = DateTime.Now
                    });
                    return;
                }
            }
            catch
            {
                // StaffHub unavailable — fall through to local fallback
            }

            // Fallback: minimal data from local UserAccount
            HttpContext.Session.SetUserSession(new UserSessionData
            {
                StaffId = staffId,
                Name = localUser.UserName,
                Email = localUser.Email ?? string.Empty,
                IsActive = localUser.Status == "ACTIVE",
                SessionCreated = DateTime.Now
            });
        }

        private IActionResult RedirectAfterLogin(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        private async Task<string?> GetAzureRedirectUrlAsync(string? returnUrl)
        {
            var baseUrl = _configuration["StaffHubApi:BaseUrl"];
            var apiKey = _configuration["StaffHubApi:ApiKey"] ?? "";

            if (string.IsNullOrEmpty(baseUrl))
                return null;

            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(baseUrl);
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

            var appBaseUrl = _configuration["App:BaseUrl"];
            var callbackUrl = $"{appBaseUrl}/Account/AzureCallback";

            if (!string.IsNullOrEmpty(returnUrl))
            {
                callbackUrl += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
            }

            var requestUrl = $"/api/auth/redirect-azure-url?returnUrl={Uri.EscapeDataString(callbackUrl)}";
            var response = await httpClient.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var redirectResponse = JsonSerializer.Deserialize<AzureRedirectResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return redirectResponse?.RedirectUrl;
            }

            return null;
        }

        private async Task<bool> ValidateStaffHubSessionAsync(string sessionId)
        {
            try
            {
                var baseUrl = _configuration["StaffHubApi:BaseUrl"];
                var apiKey = _configuration["StaffHubApi:ApiKey"] ?? "";

                if (string.IsNullOrEmpty(baseUrl))
                {
                    // _logger.LogError("ValidateStaffHubSessionAsync: StaffHub API base URL is not configured");
                    return false;
                }

                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(baseUrl);
                httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
                httpClient.DefaultRequestHeaders.Add("SessionId", sessionId);

                var response = await httpClient.GetAsync("/api/auth/validate");
                if (!response.IsSuccessStatusCode)
                {
                    // _logger.LogWarning("ValidateStaffHubSessionAsync: StaffHub returned {StatusCode} for SessionId {SessionId}",
                    //     (int)response.StatusCode, sessionId);
                }
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                // _logger.LogError(ex, "ValidateStaffHubSessionAsync: error calling StaffHub for SessionId {SessionId}", sessionId);
                return false;
            }
        }
    }
}