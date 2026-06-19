using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public class ChatResponseDto
    {
        public string Answer { get; set; } = string.Empty;
        public List<string> Sources { get; set; } = new();
        public string SessionId { get; set; } = string.Empty;
    }
}
