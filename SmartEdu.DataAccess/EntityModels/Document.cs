using SmartEdu.Shared.Enums;

namespace SmartEdu.DataAccess.EntityModels
{
    public class Document : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;   // tên file gốc
        public string FilePath { get; set; } = string.Empty;   // đường dẫn lưu trên server
        public string FileType { get; set; } = string.Empty;   // "pdf" | "docx"
        public long FileSize { get; set; }

        public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

        public int SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;

        public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
        public ICollection<DocumentLog> Logs { get; set; } = new List<DocumentLog>();
    }
}
