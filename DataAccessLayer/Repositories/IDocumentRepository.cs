using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories
{
    /// <summary>
    /// Interface định nghĩa các thao tác dữ liệu cho Document
    /// </summary>
    public interface IDocumentRepository
    {
        /// <summary>Lấy tất cả tài liệu, tuỳ chọn bao gồm cả đã xoá mềm</summary>
        Task<IEnumerable<Document>> GetAllAsync(bool includeDeleted = false);

        /// <summary>Lấy tài liệu theo Id</summary>
        Task<Document?> GetByIdAsync(int id, bool includeDeleted = false);

        /// <summary>Lấy danh sách tài liệu theo SubjectId</summary>
        Task<IEnumerable<Document>> GetBySubjectIdAsync(int subjectId, bool includeDeleted = false);

        /// <summary>Lấy danh sách tài liệu theo ChapterId</summary>
        Task<IEnumerable<Document>> GetByChapterIdAsync(int chapterId, bool includeDeleted = false);

        /// <summary>Thêm tài liệu mới vào database</summary>
        Task<Document> AddAsync(Document document);

        /// <summary>Cập nhật thông tin tài liệu</summary>
        Task<Document> UpdateAsync(Document document);

        /// <summary>Xoá mềm tài liệu (set IsDeleted = true)</summary>
        Task<bool> SoftDeleteAsync(int id);

        /// <summary>Cập nhật trạng thái xử lý của tài liệu</summary>
        Task<bool> UpdateStatusAsync(int id, DocumentStatus status);
    }
}
