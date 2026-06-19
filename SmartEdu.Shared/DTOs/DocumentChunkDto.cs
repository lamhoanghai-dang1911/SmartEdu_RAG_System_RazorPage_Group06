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
    }
}
