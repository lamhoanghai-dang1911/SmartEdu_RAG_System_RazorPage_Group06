using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.Settings
{
    public class GeminiSettings { public string ApiKey { get; set; } = string.Empty; }

    public class HuggingFaceSettings
    {
        public string Token { get; set; } = string.Empty;
        public string ChatModel { get; set; } = string.Empty;
    }

    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string User { get; set; } = string.Empty;
        public string Pass { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
    }
}
