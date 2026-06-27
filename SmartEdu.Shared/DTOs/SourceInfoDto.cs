namespace SmartEdu.Shared.DTOs
{
    /// <summary>
    /// Thông tin trích dẫn nguồn tài liệu từ AI response
    /// Format hiển thị dựa trên SourceType:
    /// - PDF: 📄 DocumentTitle · Trang X
    /// - DOCX: 📝 DocumentTitle · Mục: SectionTitle (hoặc chỉ DocumentTitle nếu không có section)
    /// - PPTX: 🖼 DocumentTitle · Slide X
    /// </summary>
    public class SourceInfoDto
    {
        /// <summary>
        /// Tên tài liệu
        /// </summary>
        public string DocumentTitle { get; set; } = string.Empty;

        /// <summary>
        /// Loại file: "pdf" | "docx" | "pptx"
        /// </summary>
        public string SourceType { get; set; } = string.Empty;

        /// <summary>
        /// Vị trí (trang hoặc slide) - dùng cho PDF và PPTX
        /// </summary>
        public int? PageNumber { get; set; }

        /// <summary>
        /// Tiêu đề section/mục - dùng cho DOCX
        /// </summary>
        public string? SectionTitle { get; set; }

        /// <summary>
        /// Trả về chuỗi formatted theo loại file
        /// Ví dụ: "📄 Design Patterns.pdf · Trang 5"
        /// </summary>
        public string GetFormattedDisplay()
        {
            string icon = SourceType?.ToLower() switch
            {
                "pdf" => "📄",
                "docx" => "📝",
                "pptx" => "🖼",
                _ => "📄"
            };

            string suffix = SourceType?.ToLower() switch
            {
                "pdf" => $"Trang {PageNumber}",
                "pptx" => $"Slide {PageNumber}",
                "docx" => !string.IsNullOrWhiteSpace(SectionTitle) ? $"Mục: {SectionTitle}" : "",
                _ => ""
            };

            if (string.IsNullOrWhiteSpace(suffix))
            {
                return $"{icon} {DocumentTitle}";
            }

            return $"{icon} {DocumentTitle} · {suffix}";
        }
    }
}
