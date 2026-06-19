using SmartEdu.Business.Interfaces;
using SmartEdu.DataAccess.EntityModels;
using SmartEdu.DataAccess.Repositories;
using SmartEdu.RazorWeb.Data;

namespace SmartEdu.Business.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public IRepository<Subject> Subjects { get; private set; }
        public IDocumentChunkRepository DocumentChunks { get; private set; }
        public IRepository<LecturerSubject> LecturerSubjects { get; private set; }
        public IRepository<DocumentLog> DocumentLogs { get; private set; }
        public IRepository<StudentSubject> StudentSubjects { get; private set; }
        public IRepository<User> Users { get; private set; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Subjects = new Repository<Subject>(_context);
            DocumentChunks = new DocumentChunkRepository(_context);
            LecturerSubjects = new Repository<LecturerSubject>(_context);
            DocumentLogs = new Repository<DocumentLog>(_context);
            StudentSubjects = new Repository<StudentSubject>(_context);
            Users = new Repository<User>(_context);
        }

        public async Task BeginTransactionAsync() => await _context.Database.BeginTransactionAsync();
        public async Task CommitTransactionAsync() => await _context.Database.CommitTransactionAsync();
        public async Task RollbackTransactionAsync() => await _context.Database.RollbackTransactionAsync();

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

        public void Dispose() => _context.Dispose();
    }
}
