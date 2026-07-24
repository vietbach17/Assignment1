using System.Collections.Generic;
using System.Threading.Tasks;

namespace BussinessLayer.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ kết nối với Gemini AI API
    /// </summary>
    public interface IGeminiService
    {
        /// <summary>
        /// Gửi yêu cầu generate content tới Gemini API kèm theo lịch sử chat và tài liệu nguồn
        /// </summary>
        /// <param name="prompt">Câu hỏi hiện tại của người dùng</param>
        /// <param name="history">Lịch sử hội thoại trước đó</param>
        /// <param name="documentPaths">Danh sách đường dẫn tuyệt đối đến tài liệu nguồn</param>
        /// <param name="restrictToDocs">Có giới hạn câu trả lời trong tài liệu không</param>
        /// <returns>Câu trả lời của Gemini AI dưới dạng text</returns>
        Task<string> GenerateContentAsync(string prompt, IEnumerable<BussinessLayer.DTOs.ChatMessageDto> history, IEnumerable<string> documentPaths, bool restrictToDocs);

        /// <summary>
        /// Lấy nội dung tài liệu đã được phân đoạn (chunk) để dễ dàng truyền vào AI hoặc xem trước
        /// </summary>
        /// <param name="path">Đường dẫn tuyệt đối đến tệp tài liệu</param>
        /// <param name="maxWords">Số từ tối đa mỗi chunk</param>
        /// <param name="overlapWords">Số từ gối đầu giữa các chunk</param>
        /// <returns>Danh sách các đoạn text của tài liệu</returns>
        Task<IEnumerable<string>> GetDocumentTextChunksAsync(string path, int maxWords = 300, int overlapWords = 50);

        /// <summary>
        /// Lấy nội dung tài liệu đã được phân đoạn, kèm theo tóm tắt ngữ cảnh tổng quát ở đầu mỗi chunk
        /// </summary>
        /// <param name="path">Đường dẫn tuyệt đối đến tệp tài liệu</param>
        /// <param name="maxWords">Số từ tối đa mỗi chunk</param>
        /// <param name="overlapWords">Số từ gối đầu giữa các chunk</param>
        /// <returns>Danh sách các đoạn text của tài liệu đã được gắn ngữ cảnh</returns>
        Task<IEnumerable<string>> GetContextualDocumentTextChunksAsync(string path, int maxWords = 300, int overlapWords = 50);

        /// <summary>
        /// Tạo embedding cho một chuỗi văn bản, để phục vụ truy vấn tìm kiếm/summarization về sau
        /// </summary>
        /// <param name="input">Nội dung văn bản cần tạo embedding</param>
        /// <returns>Vector embedding của Gemini, nếu thất bại trả về null</returns>
        Task<IEnumerable<float>?> CreateTextEmbeddingAsync(string input);

        /// <summary>
        /// Trích xuất nội dung văn bản thô từ tài liệu nguồn để học sinh xem trước những gì AI sẽ đọc
        /// </summary>
        /// <param name="path">Đường dẫn tuyệt đối đến tệp tài liệu</param>
        /// <returns>Nội dung văn bản thô đã trích xuất</returns>
        Task<string> GetDocumentTextAsync(string path);
    }
}
