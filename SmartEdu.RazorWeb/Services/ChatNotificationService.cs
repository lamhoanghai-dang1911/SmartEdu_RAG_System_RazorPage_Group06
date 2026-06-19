using Microsoft.AspNetCore.SignalR;
using SmartEdu.Business.Interfaces;
using SmartEdu.RazorWeb.Hubs;

namespace SmartEdu.RazorWeb.Services
{
    public class ChatNotificationService
    : IChatNotificationService
    {
        private readonly IHubContext<ChatHub> _hub;

        public ChatNotificationService(
            IHubContext<ChatHub> hub)
        {
            _hub = hub;
        }

        public async Task SendAnswerAsync(
            string sessionId,
            string answer)
        {
            await _hub.Clients.Group(sessionId)
                .SendAsync(
                    "ReceiveAnswer",
                    answer
                );
        }

        public async Task SendTitleUpdatedAsync(string sessionId, string newTitle)
        {
            await _hub.Clients.Group(sessionId).SendAsync("TitleUpdated", sessionId, newTitle);
        }
    }
}
