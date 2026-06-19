using Microsoft.AspNetCore.SignalR;

namespace SmartEdu.RazorWeb.Hubs
{
    public class SubjectHub : Hub
    {
        public async Task JoinSubjectGroup(string subjectId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, subjectId);
            // Send a test message to the group so clients can verify group delivery
            try
            {
                await Clients.Group(subjectId).SendAsync("DocumentLog", 0, "Hệ thống: Client đã join group thành công và đường truyền đã thông!", "System");
            }
            catch (Exception ex)
            {
                // avoid throwing from hub join; log for diagnostics
                System.Diagnostics.Debug.WriteLine($"SubjectHub.JoinSubjectGroup: failed to send test log to group {subjectId}: {ex}");
            }
        }
    }
}
