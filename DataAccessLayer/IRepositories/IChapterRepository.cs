using DataAccessLayer.Models;

namespace DataAccessLayer.IRepositories
{
    /// <summary>
    /// Interface định nghĩa các phương thức truy cập dữ liệu cho Chapter entity
    /// </summary>
    public interface IChapterRepository
    {
        /// <summary>
        /// Lấy tất cả các Chapter từ database
        /// </summary>
        /// <param name="includeDeleted">Có bao gồm các Chapter đã bị xóa mềm hay không</param>
        /// <returns>Danh sách tất cả các Chapter</returns>
        Task<IEnumerable<Chapter>> GetAllAsync(bool includeDeleted = false);

        /// <summary>
        /// Lấy tất cả các Chapter thuộc một Subject cụ thể
        /// </summary>
        /// <param name="subjectId">ID của Subject</param>
        /// <param name="includeDeleted">Có bao gồm các Chapter đã bị xóa mềm hay không</param>
        /// <returns>Danh sách các Chapter thuộc Subject, được sắp xếp theo ChapterNumber</returns>
        Task<IEnumerable<Chapter>> GetBySubjectIdAsync(int subjectId, bool includeDeleted = false);

        /// <summary>
        /// Lấy một Chapter theo ID
        /// </summary>
        /// <param name="id">ID của Chapter</param>
        /// <param name="includeDeleted">Có bao gồm Chapter đã bị xóa mềm hay không</param>
        /// <returns>Chapter nếu tìm thấy, null nếu không tìm thấy</returns>
        Task<Chapter?> GetByIdAsync(int id, bool includeDeleted = false);

        /// <summary>
        /// Lấy một Chapter theo ID kèm theo thông tin Subject
        /// </summary>
        /// <param name="id">ID của Chapter</param>
        /// <param name="includeDeleted">Có bao gồm Chapter đã bị xóa mềm hay không</param>
        /// <returns>Chapter với Subject navigation property được load, null nếu không tìm thấy</returns>
        Task<Chapter?> GetByIdWithSubjectAsync(int id, bool includeDeleted = false);

        /// <summary>
        /// Kiểm tra xem ChapterNumber đã tồn tại trong một Subject hay chưa
        /// </summary>
        /// <param name="subjectId">ID của Subject</param>
        /// <param name="chapterNumber">Số thứ tự Chapter cần kiểm tra</param>
        /// <param name="excludeId">ID của Chapter cần loại trừ khỏi kiểm tra (dùng khi update)</param>
        /// <returns>True nếu ChapterNumber đã tồn tại, False nếu chưa</returns>
        Task<bool> ChapterNumberExistsAsync(int subjectId, int chapterNumber, int? excludeId = null);

        /// <summary>
        /// Tạo mới một Chapter trong database
        /// </summary>
        /// <param name="chapter">Chapter entity cần tạo</param>
        /// <returns>Chapter đã được tạo với ID được gán</returns>
        Task<Chapter> CreateAsync(Chapter chapter);

        /// <summary>
        /// Cập nhật thông tin một Chapter trong database
        /// </summary>
        /// <param name="chapter">Chapter entity với thông tin đã được cập nhật</param>
        /// <returns>Chapter đã được cập nhật</returns>
        Task<Chapter> UpdateAsync(Chapter chapter);

        /// <summary>
        /// Xóa mềm một Chapter (đánh dấu IsDeleted = true)
        /// </summary>
        /// <param name="id">ID của Chapter cần xóa</param>
        /// <returns>True nếu xóa thành công, False nếu không tìm thấy hoặc đã bị xóa</returns>
        Task<bool> SoftDeleteAsync(int id);

        /// <summary>
        /// Khôi phục một Chapter đã bị xóa mềm (đánh dấu IsDeleted = false)
        /// </summary>
        /// <param name="id">ID của Chapter cần khôi phục</param>
        /// <returns>True nếu khôi phục thành công, False nếu không tìm thấy hoặc chưa bị xóa</returns>
        Task<bool> RestoreAsync(int id);
    }
}
