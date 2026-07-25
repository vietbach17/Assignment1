namespace BussinessLayer.DTOs
{
    /// <summary>
    /// Một đoạn (chunk) văn bản của tài liệu, kèm metadata phục vụ RAG.
    /// Dùng chung cho cả xử lý runtime lẫn lưu/đọc file chunks_*.json.
    /// </summary>
    public class DocChunk
    {
        /// <summary>Vị trí của chunk trong tài liệu (0-based).</summary>
        public int Index { get; set; }

        /// <summary>Nội dung SẠCH của chunk — đây là phần được đem đi tạo embedding.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>Số trang (PDF) hoặc số slide (PPTX) mà chunk bắt đầu. Null nếu không xác định (docx/txt).</summary>
        public int? Page { get; set; }

        /// <summary>Vector embedding của chunk (có thể null nếu chưa/không tạo được).</summary>
        public float[]? Embedding { get; set; }
    }
}
