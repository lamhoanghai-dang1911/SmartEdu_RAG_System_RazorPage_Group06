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

        /// <summary>
        /// Danh sách các nguồn tài liệu được sử dụng để trả lời
        /// Mỗi source chứa thông tin chi tiết: DocumentTitle, SourceType, PageNumber/SectionTitle
        /// </summary>
        public List<SourceInfoDto> Sources { get; set; } = new();

        public string SessionId { get; set; } = string.Empty;
    }
}
