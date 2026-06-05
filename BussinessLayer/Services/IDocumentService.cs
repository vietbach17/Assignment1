using BussinessLayer.DTOs;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Interface định nghĩa business logic cho Document
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>Lấy tất cả tài liệu</summary>
        Task<IEnumerable<DocumentDto>> GetAllDocumentsAsync(bool includeDeleted = false);

        /// <summary>Lấy tài liệu theo Id</summary>
        Task<DocumentDto?> GetDocumentByIdAsync(int id);

        /// <summary>Lấy danh sách tài liệu thuộc một Subject</summary>
        Task<IEnumerable<DocumentDto>> GetDocumentsBySubjectAsync(int subjectId);

        Task<IEnumerable<DocumentDto>> GetDocumentsByChapterAsync(int chapterId);

        Task<IEnumerable<DocumentDto>> GetDocumentsByUploadedByUserAsync(int uploadedByUserId);

        /// <summary>
        /// Upload tài liệu mới: validate file → lưu lên disk → lưu metadata vào DB
        /// </summary>
        /// <param name="viewModel">Form data từ Upload view</param>
        /// <param name="uploadedByUserId">Id của user thực hiện upload</param>
        /// <param name="wwwrootPath">Đường dẫn tuyệt đối đến thư mục wwwroot</param>
        /// <returns>(success, message, documentDto)</returns>
        Task<(bool Success, string Message, DocumentDto? Document)> UploadDocumentAsync(
            DocumentUploadViewModel viewModel,
            int uploadedByUserId,
            string wwwrootPath);

        /// <summary>
        /// Xoá tài liệu: soft delete DB record + xoá file vật lý khỏi disk
        /// </summary>
        /// <param name="id">Id của tài liệu</param>
        /// <param name="wwwrootPath">Đường dẫn tuyệt đối đến thư mục wwwroot</param>
        /// <returns>(success, message)</returns>
        Task<(bool Success, string Message)> DeleteDocumentAsync(int id, string wwwrootPath);

        /// <summary>
        /// Cập nhật trạng thái xử lý của tài liệu: Pending / Indexed / Failed
        /// </summary>
        Task<(bool Success, string Message)> UpdateDocumentStatusAsync(int id, DocumentStatus newStatus);

        /// <summary>Tìm tài liệu theo mã băm SHA-256 nội dung</summary>
        Task<DocumentDto?> GetDocumentByHashAsync(string fileHash);

        /// <summary>
        /// Cập nhật thông tin tài liệu: tiêu đề, chapter
        /// </summary>
        /// <param name="viewModel">Dữ liệu chỉnh sửa từ form</param>
        /// <returns>(success, message, documentDto)</returns>
        Task<(bool Success, string Message, DocumentDto? Document)> UpdateDocumentAsync(DocumentEditViewModel viewModel);

        /// <summary>Xử lý upload phân đoạn (Chunk Upload)</summary>
        Task<(bool Success, string Message, DocumentDto? Document)> ProcessChunkAsync(
            Microsoft.AspNetCore.Http.IFormFile chunk, int chunkIndex, int totalChunks, string fileName, string fileGuid,
            string title, int subjectId, int? chapterId, int uploadedByUserId, string wwwrootPath);
    }
}
