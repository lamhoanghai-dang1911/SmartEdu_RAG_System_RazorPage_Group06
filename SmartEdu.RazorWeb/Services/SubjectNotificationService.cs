using Microsoft.AspNetCore.SignalR;
using SmartEdu.Business.Interfaces;
using SmartEdu.RazorWeb.Hubs;

namespace SmartEdu.RazorWeb.Services
{
    public class SubjectNotificationService
    : ISubjectNotificationService
    {
        private readonly IHubContext<SubjectHub> _hub;

        public SubjectNotificationService(
            IHubContext<SubjectHub> hub)
        {
            _hub = hub;
        }

        public async Task SubjectCreated(
            int subjectId,
            string subjectName)
        {
            await _hub.Clients.All.SendAsync(
                "SubjectCreated",
                subjectId,
                subjectName);
        }

        public async Task SubjectUpdated(
            int subjectId,
            string subjectName)
        {
            await _hub.Clients.All.SendAsync(
                "SubjectUpdated",
                subjectId,
                subjectName);
        }

        public async Task SubjectDeleted(
            int subjectId)
        {
            await _hub.Clients.All.SendAsync(
                "SubjectDeleted",
                subjectId);
        }

        public async Task StudentAssigned(
            int subjectId,
            int studentId)
        {
            await _hub.Clients.Group(
                subjectId.ToString())
                .SendAsync(
                    "StudentAssigned",
                    studentId);
        }

        public async Task StudentRemoved(
            int subjectId,
            int studentId)
        {
            await _hub.Clients.Group(
                subjectId.ToString())
                .SendAsync(
                    "StudentRemoved",
                    studentId);
        }

        public async Task LecturerAssigned(
            int subjectId,
            int lecturerId)
        {
            await _hub.Clients.Group(
                subjectId.ToString())
                .SendAsync(
                    "LecturerAssigned",
                    lecturerId);
        }

        public async Task ImportCompleted(
            int subjectId,
            int totalStudents)
        {
            await _hub.Clients.Group(
                subjectId.ToString())
                .SendAsync(
                    "ImportCompleted",
                    totalStudents);
        }
    }
}
