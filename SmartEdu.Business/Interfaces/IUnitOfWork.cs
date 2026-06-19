using SmartEdu.DataAccess.EntityModels;
using SmartEdu.DataAccess.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Subject> Subjects { get; }
        IDocumentChunkRepository DocumentChunks { get; }
        IRepository<LecturerSubject> LecturerSubjects { get; }
        IRepository<DocumentLog> DocumentLogs { get; }
        IRepository<StudentSubject> StudentSubjects { get; }
        IRepository<User> Users { get; }
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task<int> SaveChangesAsync();
    }
}
