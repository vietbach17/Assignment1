namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO chứa thông tin tin nhắn trong lịch sử chat
    /// </summary>
    public class ChatMessageDto
    {
        /// <summary>
        /// Vai trò người gửi: "user" hoặc "model"
        /// </summary>
        public string Role { get; set; } = null!;

        /// <summary>
        /// Nội dung tin nhắn văn bản
        /// </summary>
        public string Text { get; set; } = null!;
    }
}
