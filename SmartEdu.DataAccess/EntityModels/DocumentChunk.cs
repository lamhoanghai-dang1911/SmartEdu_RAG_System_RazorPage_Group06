using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.DataAccess.EntityModels
{
    public class DocumentChunk : BaseEntity
    {
        public int DocumentId { get; set; }
        public Document Document { get; set; } = null!;

        public int ChunkIndex { get; set; }          // thứ tự chunk
        public string Content { get; set; } = string.Empty;  // nội dung text
        public string? EmbeddingJson { get; set; }   // vector lưu dạng JSON (float[])
        public string? EmbeddingModel { get; set; }  // "openai" | "phobert" | "e5"

        // Thông tin trích dẫn nguồn (source citation)
        public int? PageNumber { get; set; }         // Vị trí trang (PDF, PPTX) - null nếu không áp dụng
        public string? SectionTitle { get; set; }    // Tiêu đề section gần nhất (DOCX)
        public string? SourceType { get; set; }      // "pdf" | "docx" | "pptx" - dùng để format display
    }
}
