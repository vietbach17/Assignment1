using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    /// <summary>
    /// Interface định nghĩa các phương thức business logic cho Chapter management
    /// </summary>
    public interface IChapterService
    {
        /// <summary>
        /// Lấy tất cả các Chapter
        /// </summary>
        /// <param name="includeDeleted">Có bao gồm các Chapter đã bị xóa mềm hay không</param>
        /// <returns>Danh sách ChapterDto</returns>
        Task<IEnumerable<ChapterDto>> GetAllChaptersAsync(bool includeDeleted = false);

        /// <summary>
        /// Lấy tất cả các Chapter thuộc một Subject cụ thể
        /// </summary>
        /// <param name="subjectId">ID của Subject</param>
        /// <param name="includeDeleted">Có bao gồm các Chapter đã bị xóa mềm hay không</param>
        /// <returns>Danh sách ChapterDto thuộc Subject</returns>
        Task<IEnumerable<ChapterDto>> GetChaptersBySubjectIdAsync(int subjectId, bool includeDeleted = false);

        /// <summary>
        /// Lấy một Chapter theo ID
        /// </summary>
        /// <param name="id">ID của Chapter</param>
        /// <param name="includeDeleted">Có bao gồm Chapter đã bị xóa mềm hay không</param>
        /// <returns>ChapterDto nếu tìm thấy, null nếu không tìm thấy</returns>
        Task<ChapterDto?> GetChapterByIdAsync(int id, bool includeDeleted = false);

        /// <summary>
        /// Lấy một Chapter theo ID kèm theo thông tin Subject
        /// </summary>
        /// <param name="id">ID của Chapter</param>
        /// <param name="includeDeleted">Có bao gồm Chapter đã bị xóa mềm hay không</param>
        /// <returns>ChapterDto với thông tin Subject, null nếu không tìm thấy</returns>
        Task<ChapterDto?> GetChapterWithSubjectAsync(int id, bool includeDeleted = false);

        /// <summary>
        /// Tạo mới một Chapter
        /// </summary>
        /// <param name="dto">Dữ liệu Chapter cần tạo</param>
        /// <param name="userId">ID của User thực hiện tạo</param>
        /// <returns>Tuple chứa Success status, Message, và ChapterDto đã tạo</returns>
        Task<(bool Success, string Message, ChapterDto? Chapter)> CreateChapterAsync(CreateChapterDto dto, int userId);

        /// <summary>
        /// Cập nhật thông tin một Chapter
        /// </summary>
        /// <param name="dto">Dữ liệu Chapter cần cập nhật</param>
        /// <param name="userId">ID của User thực hiện cập nhật</param>
        /// <returns>Tuple chứa Success status, Message, và ChapterDto đã cập nhật</returns>
        Task<(bool Success, string Message, ChapterDto? Chapter)> UpdateChapterAsync(UpdateChapterDto dto, int userId);

        /// <summary>
        /// Xóa mềm một Chapter (đánh dấu IsDeleted = true)
        /// </summary>
        /// <param name="id">ID của Chapter cần xóa</param>
        /// <returns>Tuple chứa Success status và Message</returns>
        Task<(bool Success, string Message)> SoftDeleteChapterAsync(int id);

        /// <summary>
        /// Khôi phục một Chapter đã bị xóa mềm
        /// </summary>
        /// <param name="id">ID của Chapter cần khôi phục</param>
        /// <returns>Tuple chứa Success status và Message</returns>
        Task<(bool Success, string Message)> RestoreChapterAsync(int id);
    }
}
