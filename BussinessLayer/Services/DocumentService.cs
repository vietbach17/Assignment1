using BussinessLayer.DTOs;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using DocumentStatusEntity = DataAccessLayer.Models.DocumentStatus;
using DocumentStatusDto = BussinessLayer.DTOs.DocumentStatus;

namespace BussinessLayer.Services
{
    /// <summary>
    /// DocumentService xử lý toàn bộ business logic cho tài liệu:
    /// - Validate file (định dạng, dung lượng, không rỗng)
    /// - Lưu file vật lý vào wwwroot/uploads
    /// - Lưu metadata vào database qua repository
    /// - Soft delete kèm xoá file vật lý
    /// - Cập nhật trạng thái Pending / Indexed / Failed
    /// </summary>
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;

        // Các định dạng file được phép upload
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".docx", ".pptx"
        };

        // Kích thước tối đa: 50 MB
        private const long MaxFileSizeBytes = 50L * 1024 * 1024;

        public DocumentService(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }

        /// <summary>
        /// Lấy tất cả tài liệu và map sang DocumentDto
        /// </summary>
        public async Task<IEnumerable<DocumentDto>> GetAllDocumentsAsync(bool includeDeleted = false)
        {
            var documents = await _documentRepository.GetAllAsync(includeDeleted);
            return documents.Select(MapToDto);
        }

        /// <summary>
        /// Lấy tài liệu theo Id
        /// </summary>
        public async Task<DocumentDto?> GetDocumentByIdAsync(int id)
        {
            var document = await _documentRepository.GetByIdAsync(id, includeDeleted: false);
            return document == null ? null : MapToDto(document);
        }

        /// <summary>
        /// Lấy danh sách tài liệu thuộc một Subject
        /// </summary>
        public async Task<IEnumerable<DocumentDto>> GetDocumentsBySubjectAsync(int subjectId)
        {
            var documents = await _documentRepository.GetBySubjectIdAsync(subjectId, includeDeleted: false);
            return documents.Select(MapToDto);
        }

        /// <summary>
        /// Upload tài liệu mới:
        /// 1. Validate: không rỗng, đúng định dạng (.pdf/.docx/.pptx), không vượt 50MB
        /// 2. Tạo tên file duy nhất bằng GUID
        /// 3. Lưu file vật lý vào {wwwroot}/uploads/
        /// 4. Lưu metadata vào database
        /// </summary>
        public async Task<(bool Success, string Message, DocumentDto? Document)> UploadDocumentAsync(
            DocumentUploadViewModel viewModel,
            int uploadedByUserId,
            string wwwrootPath)
        {
            var file = viewModel.File;

            // điều kiện upload
            // 1. File không được rỗng
            if (file == null || file.Length == 0)
                return (false, "Vui lòng chọn file để tải lên.", null);

            // 2. Kiểm tra định dạng file
            var extension = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(extension))
                return (false, $"Định dạng file không hỗ trợ. Chỉ chấp nhận: PDF, DOCX, PPTX.", null);

            // 3. Kiểm tra dung lượng file (≤ 50 MB)
            if (file.Length > MaxFileSizeBytes)
                return (false, $"File vượt dung lượng cho phép. Tối đa 50 MB (file hiện tại: {file.Length / (1024.0 * 1024):F1} MB).", null);

            // 4. Kiểm tra file trùng lặp (chống duplicate)
            var existingDocs = await _documentRepository.GetBySubjectIdAsync(viewModel.SubjectId, includeDeleted: false);
            bool isDuplicate = existingDocs.Any(d => d.FileName.Equals(file.FileName, StringComparison.OrdinalIgnoreCase) && d.FileSize == file.Length);
            if (isDuplicate)
                return (false, $"Tài liệu \"{file.FileName}\" đã tồn tại trong hệ thống. Không được phép tải lên file trùng lặp.", null);

            // --- Lưu file vật lý ---

            // Đảm bảo thư mục uploads/ tồn tại
            var uploadsFolder = Path.Combine(wwwrootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            // Tạo tên file unique bằng GUID để tránh xung đột
            var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var fullFilePath = Path.Combine(uploadsFolder, storedFileName);
            var relativeFilePath = Path.Combine("uploads", storedFileName).Replace("\\", "/");

            try
            {
                using var stream = new FileStream(fullFilePath, FileMode.Create);
                await file.CopyToAsync(stream);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi lưu file: {ex.Message}", null);
            }

            // --- Lưu metadata vào DB ---

            var document = new Document
            {
                Title = viewModel.Title.Trim(),
                FileName = file.FileName,
                StoredFileName = storedFileName,
                FilePath = relativeFilePath,
                FileSize = file.Length,
                FileType = extension.TrimStart('.').ToLowerInvariant(),
                Status = DocumentStatusEntity.Pending,
                SubjectId = viewModel.SubjectId,
                ChapterId = viewModel.ChapterId,
                UploadedByUserId = uploadedByUserId,
                UploadedDate = DateTime.UtcNow,
                IsDeleted = false
            };

            var savedDocument = await _documentRepository.AddAsync(document);

            // Reload với navigation properties
            var savedDto = await GetDocumentByIdAsync(savedDocument.Id);

            return (true, $"Tài liệu \"{document.Title}\" đã được tải lên thành công.", savedDto);
        }

        /// <summary>
        /// Xoá tài liệu:
        /// 1. Soft delete record trong DB (IsDeleted = true)
        /// 2. Xoá file vật lý khỏi disk
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteDocumentAsync(int id, string wwwrootPath)
        {
            var document = await _documentRepository.GetByIdAsync(id, includeDeleted: false);
            if (document == null)
                return (false, "Không tìm thấy tài liệu.");

            var documentTitle = document.Title;

            // 1. Soft delete trong DB
            var deleted = await _documentRepository.SoftDeleteAsync(id);
            if (!deleted)
                return (false, "Xoá tài liệu thất bại.");

            // 2. Xoá file vật lý khỏi disk
            var fullFilePath = Path.Combine(wwwrootPath, document.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (File.Exists(fullFilePath))
            {
                try
                {
                    File.Delete(fullFilePath);
                }
                catch
                {
                    // Nếu xoá file thất bại, vẫn trả về success vì DB đã được cập nhật
                    // Log lỗi nếu có logging service
                }
            }

            return (true, $"Tài liệu \"{documentTitle}\" đã được xoá thành công.");
        }

        /// <summary>
        /// Cập nhật trạng thái xử lý của tài liệu: Pending → Indexed hoặc Failed
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateDocumentStatusAsync(int id, DocumentStatusDto newStatus)
        {
            var updated = await _documentRepository.UpdateStatusAsync(id, (DocumentStatusEntity)newStatus);
            if (!updated)
                return (false, "Không tìm thấy tài liệu để cập nhật trạng thái.");

            var statusName = newStatus switch
            {
                DocumentStatusDto.Pending => "Pending",
                DocumentStatusDto.Indexed => "Indexed",
                DocumentStatusDto.Failed  => "Failed",
                _                      => newStatus.ToString()
            };

            return (true, $"Trạng thái tài liệu đã được cập nhật thành \"{statusName}\".");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helper: Map Document entity → DocumentDto
        // ─────────────────────────────────────────────────────────────────────

        private static DocumentDto MapToDto(Document d) => new()
        {
            Id                = d.Id,
            Title             = d.Title,
            FileName          = d.FileName,
            StoredFileName    = d.StoredFileName,
            FilePath          = d.FilePath,
            FileSize          = d.FileSize,
            FileType          = d.FileType,
            Status            = (DocumentStatusDto)d.Status,
            SubjectId         = d.SubjectId,
            SubjectCode       = d.Subject?.SubjectCode,
            SubjectName       = d.Subject?.SubjectName,
            ChapterId         = d.ChapterId,
            ChapterTitle      = d.Chapter?.ChapterTitle,
            UploadedByUserId  = d.UploadedByUserId,
            UploadedByUsername = d.UploadedBy?.Username,
            UploadedDate      = d.UploadedDate,
            IsDeleted         = d.IsDeleted
        };
    }
}
