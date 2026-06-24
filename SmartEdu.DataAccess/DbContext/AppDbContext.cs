using Microsoft.EntityFrameworkCore;
using SmartEdu.DataAccess.EntityModels;

namespace SmartEdu.RazorWeb.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Document> Documents => Set<Document>();
        public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
        public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<User> Users => Set<User>();
        public DbSet<LecturerSubject> LecturerSubjects => Set<LecturerSubject>();
        public DbSet<DocumentLog> DocumentLogs => Set<DocumentLog>();
        public DbSet<StudentSubject> StudentSubjects => Set<StudentSubject>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Cấu hình Filter & Index
            modelBuilder.Entity<Document>().HasQueryFilter(d => !d.IsDeleted);
            modelBuilder.Entity<DocumentChunk>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<DocumentChunk>().HasIndex(c => c.DocumentId);
            modelBuilder.Entity<ChatMessage>().HasIndex(m => m.ChatSessionId);

            // 2. Cấu hình User
            modelBuilder.Entity<User>().Property(u => u.Role).HasConversion<string>();

            // 3. Cấu hình StudentSubject (Many-to-Many)
            modelBuilder.Entity<StudentSubject>(entity =>
            {
                entity.HasKey(ss => new { ss.StudentId, ss.SubjectId });
                entity.HasOne(ss => ss.User).WithMany().HasForeignKey(ss => ss.StudentId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(ss => ss.Subject).WithMany().HasForeignKey(ss => ss.SubjectId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LecturerSubject>(entity =>
            {
                entity.ToTable("LecturerSubject");
                entity.HasKey(ls => new { ls.LecturerId, ls.SubjectId });
                entity.HasOne(ls => ls.Lecturer).WithMany().HasForeignKey(ls => ls.LecturerId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(ls => ls.Subject).WithMany().HasForeignKey(ls => ls.SubjectId).OnDelete(DeleteBehavior.Cascade);
            });

            // 5. Cấu hình DocumentLog
            modelBuilder.Entity<DocumentLog>(entity =>
            {
                entity.ToTable("DocumentLog");
                entity.HasOne(d => d.Document)
                      .WithMany(d => d.Logs)
                      .HasForeignKey(d => d.DocumentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}