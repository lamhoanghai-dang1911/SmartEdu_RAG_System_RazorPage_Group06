using SmartEdu.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface ISubjectService
    {
        Task<IEnumerable<SubjectDto>> GetAllAsync();
        Task<SubjectDto?> GetByIdAsync(int id);
        Task CreateAsync(SubjectCreateDto dto);
        Task UpdateAsync(SubjectUpdateDto dto);
        Task<IEnumerable<SubjectDto>> GetSubjectsByUserIdAsync(int userId);
        Task DeleteAsync(int id);
        Task AssignStudentToSubject(int studentId, int subjectId);
        Task RemoveStudentFromSubject(int studentId, int subjectId);
        Task<(IEnumerable<UserDto> Enrolled, IEnumerable<UserDto> NotEnrolled)> GetStudentEnrollmentStatus(int subjectId);
        Task ImportStudentsAsync(int subjectId, List<StudentImportDto> importedStudents);
        Task AssignLecturerToSubject(AssignLecturerDto dto);
        Task SetLeaderAsync(int subjectId, int lecturerId);
        Task RemoveLeaderAsync(int subjectId, int lecturerId);
        Task<(IEnumerable<UserDto> Assigned, IEnumerable<UserDto> NotAssigned)> GetLecturerAssignmentStatus(int subjectId);
        Task<IEnumerable<SubjectDto>> GetSubjectsByLecturerIdAsync(int lecturerId);
        Task<bool> CanUploadDocument(int lecturerId, int subjectId);
        Task RemoveLecturerFromSubject(int lecturerId, int subjectId);
    }
}
