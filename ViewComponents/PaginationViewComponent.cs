using Microsoft.AspNetCore.Mvc;
using mvc_boilerplate.Models;
using mvc_boilerplate.Models.UI;

namespace mvc_boilerplate.ViewComponents
{
    public class PaginationViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(PaginationInfo paginationInfo)
        {
            return View(paginationInfo);
        }
    }
}