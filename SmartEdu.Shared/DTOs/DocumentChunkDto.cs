using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public class DocumentChunkDto
    {
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public string DocumentTitle { get; set; } = string.Empty;

        public int CharLength { get; set; }
        public int TotalChunks { get; set; }

        // Thông tin kỹ thuật
        public int CharStart { get; set; }
        public int CharEnd { get; set; }
        public int OverlapSize { get; set; }
    }
}
