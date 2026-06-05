using BussinessLayer.DTOs;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Interface định nghĩa các phương thức business logic cho Subject management
    /// </summary>
    public interface ISubjectService
    {
        /// <summary>
        /// Lấy tất cả các Subject
        /// </summary>
        /// <param name="includeDeleted">Có bao gồm các Subject đã bị xóa mềm hay không</param>
        /// <returns>Danh sách SubjectDto</returns>
        Task<IEnumerable<SubjectDto>> GetAllSubjectsAsync(bool includeDeleted = false);

        /// <summary>
        /// Lay cac Subject duoc gan cho mot Lecturer cu the
        /// </summary>
        /// <param name="lecturerId">ID cua Lecturer</param>
        /// <param name="includeDeleted">Co bao gom cac Subject da bi xoa mem hay khong</param>
        /// <returns>Danh sach SubjectDto</returns>
        Task<IEnumerable<SubjectDto>> GetSubjectsByLecturerIdAsync(int lecturerId, bool includeDeleted = false);

        /// <summary>
        /// Lấy Subject theo ID
        /// </summary>
        /// <param name="id">ID của Subject</param>
        /// <param name="includeDeleted">Có bao gồm Subject đã bị xóa mềm hay không</param>
        /// <returns>SubjectDto hoặc null nếu không tìm thấy</returns>
        Task<SubjectDto?> GetSubjectByIdAsync(int id, bool includeDeleted = false);

        /// <summary>
        /// Lấy Subject theo ID kèm theo danh sách Chapters
        /// </summary>
        /// <param name="id">ID của Subject</param>
        /// <param name="includeDeleted">Có bao gồm Subject đã bị xóa mềm hay không</param>
        /// <returns>SubjectDto với Chapters hoặc null nếu không tìm thấy</returns>
        Task<SubjectDto?> GetSubjectWithChaptersAsync(int id, bool includeDeleted = false);

        /// <summary>
        /// Tạo mới Subject
        /// </summary>
        /// <param name="dto">Dữ liệu Subject cần tạo</param>
        /// <param name="userId">ID của User đang thực hiện thao tác</param>
        /// <returns>Tuple (Success, Message, Subject)</returns>
        Task<(bool Success, string Message, SubjectDto? Subject)> CreateSubjectAsync(CreateSubjectDto dto, int userId);

        /// <summary>
        /// Cập nhật Subject
        /// </summary>
        /// <param name="dto">Dữ liệu Subject cần cập nhật</param>
        /// <param name="userId">ID của User đang thực hiện thao tác</param>
        /// <returns>Tuple (Success, Message, Subject)</returns>
        Task<(bool Success, string Message, SubjectDto? Subject)> UpdateSubjectAsync(UpdateSubjectDto dto, int userId);

        /// <summary>
        /// Xóa mềm Subject (đánh dấu IsDeleted = true)
        /// </summary>
        /// <param name="id">ID của Subject cần xóa</param>
        /// <returns>Tuple (Success, Message)</returns>
        Task<(bool Success, string Message)> SoftDeleteSubjectAsync(int id);

        /// <summary>
        /// Khôi phục Subject đã bị xóa mềm
        /// </summary>
        /// <param name="id">ID của Subject cần khôi phục</param>
        /// <returns>Tuple (Success, Message)</returns>
        Task<(bool Success, string Message)> RestoreSubjectAsync(int id);

        /// <summary>
        /// Kiểm tra xem Lecturer có được gán cho Subject hay không
        /// </summary>
        /// <param name="subjectId">ID của Subject</param>
        /// <param name="lecturerId">ID của Lecturer</param>
        /// <returns>True nếu Lecturer được gán cho Subject</returns>
        Task<bool> IsLecturerAssignedToSubjectAsync(int subjectId, int lecturerId);

        /// <summary>
        /// Gán Lecturer cho Subject
        /// </summary>
        /// <param name="subjectId">ID của Subject</param>
        /// <param name="lecturerId">ID của Lecturer</param>
        Task AssignLecturerAsync(int subjectId, int lecturerId);

        /// <summary>
        /// Xóa tất cả lecturer assignments cho một Subject
        /// </summary>
        /// <param name="subjectId">ID của Subject</param>
        Task ClearLecturerAssignmentsAsync(int subjectId);

        /// <summary>
        /// Lấy danh sách tất cả Lecturers
        /// </summary>
        /// <returns>Danh sách UserDto với role Lecturer</returns>
        Task<IEnumerable<UserDto>> GetAllLecturersAsync();

        /// <summary>
        /// Lấy danh sách Lecturers được assign cho một Subject
        /// </summary>
        /// <param name="subjectId">ID của Subject</param>
        /// <returns>Danh sách UserDto</returns>
        Task<IEnumerable<UserDto>> GetAssignedLecturersAsync(int subjectId);
    }
}
