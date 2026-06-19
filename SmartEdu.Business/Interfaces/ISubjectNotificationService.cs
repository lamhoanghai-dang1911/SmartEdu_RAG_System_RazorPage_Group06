using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface ISubjectNotificationService
    {
        Task SubjectCreated(int subjectId, string subjectName);

        Task SubjectUpdated(int subjectId, string subjectName);

        Task SubjectDeleted(int subjectId);

        Task StudentAssigned(
            int subjectId,
            int studentId);

        Task StudentRemoved(
            int subjectId,
            int studentId);

        Task LecturerAssigned(
            int subjectId,
            int lecturerId);

        Task ImportCompleted(
            int subjectId,
            int totalStudents);
    }
}
