using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface IChatNotificationService
    {
        Task SendAnswerAsync(string sessionId, string answer);
        Task SendTitleUpdatedAsync(string sessionId, string newTitle);
        Task SendSessionCreatedAsync(int userId, string sessionId, string title, string? subjectName);
    }
}
