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

        public async Task<IEnumerable<DocumentDto>> GetDocumentsByChapterAsync(int chapterId)
        {
            var documents = await _documentRepository.GetByChapterIdAsync(chapterId, includeDeleted: false);
            return documents.Select(MapToDto);
        }

        public async Task<IEnumerable<DocumentDto>> GetDocumentsByUploadedByUserAsync(int uploadedByUserId)
        {
            var documents = await _documentRepository.GetByUploadedByUserIdAsync(uploadedByUserId, includeDeleted: false);
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

            // 4. Tính toán mã băm SHA-256 và kiểm tra trùng lặp nội dung
            string fileHash;
            try
            {
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    using (var stream = file.OpenReadStream())
                    {
                        var hashBytes = sha256.ComputeHash(stream);
                        fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi tính mã băm file: {ex.Message}", null);
            }

            var duplicateDoc = await _documentRepository.GetByHashAsync(fileHash);
            if (duplicateDoc != null)
                return (false, $"Tài liệu trùng lặp nội dung với tài liệu \"{duplicateDoc.Title}\" đã tồn tại trong hệ thống.", null);

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
                Status = DocumentStatusEntity.Indexed,
                SubjectId = viewModel.SubjectId,
                ChapterId = viewModel.ChapterId,
                UploadedByUserId = uploadedByUserId,
                UploadedDate = DateTime.UtcNow,
                FileHash = fileHash,
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

        /// <summary>Tìm tài liệu theo mã băm SHA-256 nội dung</summary>
        public async Task<DocumentDto?> GetDocumentByHashAsync(string fileHash)
        {
            var document = await _documentRepository.GetByHashAsync(fileHash);
            return document == null ? null : MapToDto(document);
        }

        /// <summary>
        /// Cập nhật thông tin tài liệu: tiêu đề và chapter
        /// </summary>
        public async Task<(bool Success, string Message, DocumentDto? Document)> UpdateDocumentAsync(DocumentEditViewModel viewModel)
        {
            var document = await _documentRepository.GetByIdAsync(viewModel.Id, includeDeleted: false);
            if (document == null)
                return (false, "Không tìm thấy tài liệu.", null);

            document.Title = viewModel.Title.Trim();
            document.ChapterId = viewModel.ChapterId;

            await _documentRepository.UpdateAsync(document);

            var updatedDto = await GetDocumentByIdAsync(document.Id);
            return (true, $"Tài liệu \"{document.Title}\" đã được cập nhật thành công.", updatedDto);
        }

        /// <summary>Xử lý upload phân đoạn (Chunk Upload)</summary>
        public async Task<(bool Success, string Message, DocumentDto? Document)> ProcessChunkAsync(
            Microsoft.AspNetCore.Http.IFormFile chunk, int chunkIndex, int totalChunks, string fileName, string fileGuid,
            string title, int subjectId, int? chapterId, int uploadedByUserId, string wwwrootPath)
        {
            if (chunk == null || chunk.Length == 0)
                return (false, "Phân đoạn file trống.", null);

            var extension = Path.GetExtension(fileName);
            if (!AllowedExtensions.Contains(extension))
                return (false, $"Định dạng file không hỗ trợ. Chỉ chấp nhận: PDF, DOCX, PPTX.", null);

            // 1. Tạo thư mục tạm lưu các chunk
            var tempFolder = Path.Combine(wwwrootPath, "temp", fileGuid);
            Directory.CreateDirectory(tempFolder);

            // 2. Lưu chunk hiện tại
            var chunkFilePath = Path.Combine(tempFolder, $"chunk_{chunkIndex}");
            try
            {
                using (var stream = new FileStream(chunkFilePath, FileMode.Create))
                {
                    await chunk.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi lưu chunk {chunkIndex}: {ex.Message}", null);
            }

            // 3. Nếu chưa phải chunk cuối cùng, trả về trạng thái đang xử lý
            if (chunkIndex < totalChunks - 1)
            {
                return (true, $"Đã tải lên phân đoạn {chunkIndex + 1}/{totalChunks}.", null);
            }

            // 4. Chunk cuối cùng -> Thực hiện ghép các chunk
            var uploadsFolder = Path.Combine(wwwrootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var fullFilePath = Path.Combine(uploadsFolder, storedFileName);
            var relativeFilePath = Path.Combine("uploads", storedFileName).Replace("\\", "/");

            try
            {
                // Ghép tệp tin
                using (var destStream = new FileStream(fullFilePath, FileMode.Create))
                {
                    for (int i = 0; i < totalChunks; i++)
                    {
                        var partPath = Path.Combine(tempFolder, $"chunk_{i}");
                        if (!File.Exists(partPath))
                        {
                            // Nếu thiếu chunk, dọn dẹp và báo lỗi
                            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
                            if (File.Exists(fullFilePath)) File.Delete(fullFilePath);
                            return (false, $"Thiếu phân đoạn thứ {i + 1}. Vui lòng upload lại.", null);
                        }

                        using (var srcStream = new FileStream(partPath, FileMode.Open))
                        {
                            await srcStream.CopyToAsync(destStream);
                        }
                    }
                }

                // Dọn dẹp thư mục tạm
                Directory.Delete(tempFolder, true);
            }
            catch (Exception ex)
            {
                if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
                if (File.Exists(fullFilePath)) File.Delete(fullFilePath);
                return (false, $"Lỗi khi ghép file: {ex.Message}", null);
            }

            // 5. Tính toán SHA-256
            string fileHash;
            long fileSize;
            try
            {
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    using (var stream = File.OpenRead(fullFilePath))
                    {
                        var hashBytes = sha256.ComputeHash(stream);
                        fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                        fileSize = stream.Length;
                    }
                }
            }
            catch (Exception ex)
            {
                if (File.Exists(fullFilePath)) File.Delete(fullFilePath);
                return (false, $"Lỗi khi tính mã băm file: {ex.Message}", null);
            }

            // 6. Kiểm tra dung lượng tối đa
            if (fileSize > MaxFileSizeBytes)
            {
                if (File.Exists(fullFilePath)) File.Delete(fullFilePath);
                return (false, $"File vượt dung lượng cho phép. Tối đa 50 MB (file hiện tại: {fileSize / (1024.0 * 1024):F1} MB).", null);
            }

            // 7. Kiểm tra trùng mã băm SHA-256
            var duplicateDoc = await _documentRepository.GetByHashAsync(fileHash);
            if (duplicateDoc != null)
            {
                if (File.Exists(fullFilePath)) File.Delete(fullFilePath);
                return (false, $"Tài liệu trùng lặp nội dung với tài liệu \"{duplicateDoc.Title}\" đã tồn tại trong hệ thống.", null);
            }

            // 8. Lưu metadata vào DB
            var document = new Document
            {
                Title = title.Trim(),
                FileName = fileName,
                StoredFileName = storedFileName,
                FilePath = relativeFilePath,
                FileSize = fileSize,
                FileType = extension.TrimStart('.').ToLowerInvariant(),
                Status = DocumentStatusEntity.Indexed,
                SubjectId = subjectId,
                ChapterId = chapterId,
                UploadedByUserId = uploadedByUserId,
                UploadedDate = DateTime.UtcNow,
                FileHash = fileHash,
                IsDeleted = false
            };

            try
            {
                var savedDocument = await _documentRepository.AddAsync(document);
                var savedDto = await GetDocumentByIdAsync(savedDocument.Id);
                return (true, $"Tài liệu \"{document.Title}\" đã được tải lên và ghép thành công.", savedDto);
            }
            catch (Exception ex)
            {
                if (File.Exists(fullFilePath)) File.Delete(fullFilePath);
                return (false, $"Lỗi khi lưu thông tin vào cơ sở dữ liệu: {ex.Message}", null);
            }
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
            FileHash          = d.FileHash,
            IsDeleted         = d.IsDeleted
        };
    }
}
