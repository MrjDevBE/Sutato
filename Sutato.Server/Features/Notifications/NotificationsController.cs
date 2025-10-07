using Microsoft.AspNetCore.Mvc;

namespace Sutato.Server.Features.Notifications
{
    public class NotificationsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
