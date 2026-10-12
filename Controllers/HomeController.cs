using Microsoft.AspNetCore.Mvc;
using mvc_boilerplate.Models.ViewModels;
using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;

namespace mvc_boilerplate.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User?.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            return RedirectToAction("Login", "Account");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            if (statusCode.HasValue)
            {
                HttpContext.Response.StatusCode = statusCode.Value;
            }
            else
            {
                var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
                if (exceptionHandlerPathFeature != null)
                    statusCode = 500;
            }

            var errorViewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = statusCode ?? HttpContext.Response.StatusCode,
                ErrorMessage = "We're sorry, something went wrong. Please try again later or contact support if the problem persists."
            };

            return View("Error", errorViewModel);
        }

        public IActionResult AccessDenied()
        {
            return Error(403);
        }
    }
}

