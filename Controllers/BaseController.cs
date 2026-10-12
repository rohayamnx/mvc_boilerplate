using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace mvc_boilerplate.Controllers
{
    [Authorize]
    public abstract class BaseController : Controller
    {
        protected bool HasPermission(string permissionClaim) =>
            User.HasClaim("Permission", permissionClaim);

        protected IActionResult HandleUnauthorizedAccess()
        {
            if (User?.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Account");

            return RedirectToAction("AccessDenied", "Home");
        }
    }
}
