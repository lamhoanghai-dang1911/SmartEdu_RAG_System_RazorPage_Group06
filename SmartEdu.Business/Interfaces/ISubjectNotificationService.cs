using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface ISubjectNotificationService
    {
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

        Task SubjectListChanged();
    }
}
