using Microsoft.AspNetCore.SignalR;
using Sutato.Server.Features.Dashboard;

namespace Sutato.Server.Features.Dashboard
{
    public class DashboardService
    {
        private readonly IHubContext<DashboardHub> _hubContext;

        public DashboardService(IHubContext<DashboardHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task RefreshKpi()
        {
            // Example data from database
            int activeUsers = 32;
            int projects = 10;
            int tasks = 45;
            int notifications = 5;

            await _hubContext.Clients.All.SendAsync("UpdateKpi", activeUsers, projects, tasks, notifications);
        }

        public async Task AddActivity(string message)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveActivity", message);
        }

        public async Task StartDemoUpdates()
        {
            var random = new Random();

            while (true)
            {
                int activeUsers = random.Next(25, 40);
                int projects = random.Next(5, 12);
                int tasks = random.Next(30, 60);
                int notifications = random.Next(1, 10);

                await _hubContext.Clients.All.SendAsync("UpdateKpi", activeUsers, projects, tasks, notifications);
                await _hubContext.Clients.All.SendAsync("ReceiveActivity", $"Updated at {DateTime.Now:T}");

                await Task.Delay(45000); // every 45 seconds
            }
        }

    }
}
