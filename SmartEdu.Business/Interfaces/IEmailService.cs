using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface IEmailService
    {
        Task SendWelcomeEmailAsync(string toEmail, string fullName, string username, string password);
        Task SendEnrollmentNotificationAsync(string toEmail, string fullName, string subjectName);
    }
}
