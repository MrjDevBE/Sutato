using Microsoft.AspNetCore.Mvc;

namespace Sutato.Server.Features.Reports
{
    public class ReportsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
