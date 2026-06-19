using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.DataAccess.EntityModels
{
    public class ChatSession : BaseEntity
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public string? Title { get; set; }
        public int? SubjectId { get; set; }
        public Subject? Subject { get; set; }
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public int UserId { get; set; }
    }

    public class ChatMessage : BaseEntity
    {
        public int ChatSessionId { get; set; }
        public ChatSession ChatSession { get; set; } = null!;

        public string Role { get; set; } = "user";  // "user" | "assistant"
        public string Content { get; set; } = string.Empty;
        public string? SourceChunkIds { get; set; } // JSON array: [1, 5, 12]
    }
}
