using BussinessLayer.DTOs;
using DataAccessLayer.Models;

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
    }
}
