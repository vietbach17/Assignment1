using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories
{
    /// <summary>
    /// Interface định nghĩa các phương thức truy cập dữ liệu cho Subject entity
    /// </summary>
    public interface ISubjectRepository
    {
        /// <summary>
        /// Lấy tất cả các Subject
        /// </summary>
        /// <param name="includeDeleted">Có bao gồm các Subject đã bị xóa mềm hay không</param>
        /// <returns>Danh sách Subject</returns>
        Task<IEnumerable<Subject>> GetAllAsync(bool includeDeleted = false);

        /// <summary>
        /// Lấy các Subject được gán cho một Lecturer cụ thể
        /// </summary>
        /// <param name="lecturerId">ID của Lecturer</param>
        /// <param name="includeDeleted">Có bao gồm các Subject đã bị xóa mềm hay không</param>
        /// <returns>Danh sách Subject của Lecturer</returns>
        Task<IEnumerable<Subject>> GetByLecturerIdAsync(int lecturerId, bool includeDeleted = false);

        /// <summary>
        /// Lấy Subject theo ID
        /// </summary>
        /// <param name="id">ID của Subject</param>
        /// <param name="includeDeleted">Có bao gồm Subject đã bị xóa mềm hay không</param>
        /// <returns>Subject hoặc null nếu không tìm thấy</returns>
        Task<Subject?> GetByIdAsync(int id, bool includeDeleted = false);

        /// <summary>
        /// Lấy Subject theo ID kèm theo danh sách Chapters
        /// </summary>
        /// <param name="id">ID của Subject</param>
        /// <param name="includeDeleted">Có bao gồm Subject đã bị xóa mềm hay không</param>
        /// <returns>Subject với Chapters hoặc null nếu không tìm thấy</returns>
        Task<Subject?> GetByIdWithChaptersAsync(int id, bool includeDeleted = false);

        /// <summary>
        /// Lấy Subject theo SubjectCode
        /// </summary>
        /// <param name="subjectCode">Mã môn học</param>
        /// <param name="includeDeleted">Có bao gồm Subject đã bị xóa mềm hay không</param>
        /// <returns>Subject hoặc null nếu không tìm thấy</returns>
        Task<Subject?> GetBySubjectCodeAsync(string subjectCode, bool includeDeleted = false);

        /// <summary>
        /// Kiểm tra SubjectCode đã tồn tại hay chưa
        /// </summary>
        /// <param name="subjectCode">Mã môn học cần kiểm tra</param>
        /// <param name="excludeId">ID của Subject cần loại trừ (dùng khi update)</param>
        /// <returns>True nếu SubjectCode đã tồn tại</returns>
        Task<bool> SubjectCodeExistsAsync(string subjectCode, int? excludeId = null);

        /// <summary>
        /// Kiểm tra Lecturer có được gán cho Subject hay không
        /// </summary>
        /// <param name="subjectId">ID của Subject</param>
        /// <param name="lecturerId">ID của Lecturer</param>
        /// <returns>True nếu Lecturer được gán cho Subject</returns>
        Task<bool> IsLecturerAssignedToSubjectAsync(int subjectId, int lecturerId);

        /// <summary>
        /// Tạo mới Subject
        /// </summary>
        /// <param name="subject">Subject cần tạo</param>
        /// <returns>Subject đã được tạo</returns>
        Task<Subject> CreateAsync(Subject subject);

        /// <summary>
        /// Cập nhật Subject
        /// </summary>
        /// <param name="subject">Subject cần cập nhật</param>
        /// <returns>Subject đã được cập nhật</returns>
        Task<Subject> UpdateAsync(Subject subject);

        /// <summary>
        /// Xóa mềm Subject (đánh dấu IsDeleted = true)
        /// </summary>
        /// <param name="id">ID của Subject cần xóa</param>
        /// <returns>True nếu xóa thành công</returns>
        Task<bool> SoftDeleteAsync(int id);

        /// <summary>
        /// Khôi phục Subject đã bị xóa mềm
        /// </summary>
        /// <param name="id">ID của Subject cần khôi phục</param>
        /// <returns>True nếu khôi phục thành công</returns>
        Task<bool> RestoreAsync(int id);

        /// <summary>
        /// Kiểm tra Subject có Chapters hay không
        /// </summary>
        /// <param name="subjectId">ID của Subject</param>
        /// <returns>True nếu Subject có ít nhất một Chapter chưa bị xóa</returns>
        Task<bool> HasChaptersAsync(int subjectId);

        /// <summary>
        /// Gán Lecturer cho Subject
        /// </summary>
        /// <param name="subjectId">ID của Subject</param>
        /// <param name="lecturerId">ID của Lecturer</param>
        Task AssignLecturerAsync(int subjectId, int lecturerId);

        /// <summary>
        /// Hủy gán Lecturer khỏi Subject
        /// </summary>
        /// <param name="subjectId">ID của Subject</param>
        /// <param name="lecturerId">ID của Lecturer</param>
        Task UnassignLecturerAsync(int subjectId, int lecturerId);

        /// <summary>
        /// Lấy danh sách Lecturers được gán cho một Subject
        /// </summary>
        /// <param name="subjectId">ID của Subject</param>
        /// <returns>Danh sách User (Lecturers)</returns>
        Task<IEnumerable<User>> GetAssignedLecturersAsync(int subjectId);
    }
}
