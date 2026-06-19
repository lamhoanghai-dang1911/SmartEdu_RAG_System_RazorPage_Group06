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
    }
}
