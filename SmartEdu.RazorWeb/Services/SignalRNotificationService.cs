using Microsoft.AspNetCore.SignalR;
using SmartEdu.Business.Interfaces;
using SmartEdu.RazorWeb.Hubs;

namespace SmartEdu.RazorWeb.Services
{
    public class SignalRNotificationService
    : INotificationService
    {
        private readonly IHubContext<SubjectHub> _hub;

        public SignalRNotificationService(
            IHubContext<SubjectHub> hub)
        {
            _hub = hub;
        }

        public async Task SubjectCreated(string subjectName)
        {
            await _hub.Clients.All.SendAsync(
                "SubjectCreated",
                subjectName
            );
        }
    }
}
