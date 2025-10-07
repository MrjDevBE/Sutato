using Microsoft.AspNetCore.Mvc;

namespace Sutato.Server.Features.Dashboard
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _dashboardService;

        public DashboardController(DashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshKpi()
        {
            await _dashboardService.RefreshKpi();
            return Ok(new { message = "KPI refreshed successfully" });
        }

        [HttpPost("activity")]
        public async Task<IActionResult> AddActivity([FromBody] string message)
        {
            await _dashboardService.AddActivity(message);
            return Ok(new { message = "Activity broadcasted successfully" });
        }
    }
}
