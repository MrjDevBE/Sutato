using Microsoft.AspNetCore.Mvc;

namespace Sutato.Server.Features.Organization
{
    public class OrganizationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
