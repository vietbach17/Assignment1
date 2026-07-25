using BussinessLayer.IServices;
using DataAccessLayer.IRepositories;
using BussinessLayer.DTOs;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Service implementation cho Chapter management business logic
    /// </summary>
    public class ChapterService : IChapterService
    {
        private readonly IChapterRepository _chapterRepository;
        private readonly ISubjectRepository _subjectRepository;

        public ChapterService(IChapterRepository chapterRepository, ISubjectRepository subjectRepository)
        {
            _chapterRepository = chapterRepository;
            _subjectRepository = subjectRepository;
        }

        public async Task<IEnumerable<ChapterDto>> GetAllChaptersAsync(bool includeDeleted = false)
        {
            var chapters = await _chapterRepository.GetAllAsync(includeDeleted);
            return chapters.Select(MapToDto);
        }

        public async Task<IEnumerable<ChapterDto>> GetChaptersBySubjectIdAsync(int subjectId, bool includeDeleted = false)
        {
            var chapters = await _chapterRepository.GetBySubjectIdAsync(subjectId, includeDeleted);
            return chapters.Select(MapToDto);
        }

        public async Task<ChapterDto?> GetChapterByIdAsync(int id, bool includeDeleted = false)
        {
            var chapter = await _chapterRepository.GetByIdAsync(id, includeDeleted);
            return chapter != null ? MapToDto(chapter) : null;
        }

        public async Task<ChapterDto?> GetChapterWithSubjectAsync(int id, bool includeDeleted = false)
        {
            var chapter = await _chapterRepository.GetByIdWithSubjectAsync(id, includeDeleted);
            return chapter != null ? MapToDtoWithSubject(chapter) : null;
        }

        public async Task<(bool Success, string Message, ChapterDto? Chapter)> CreateChapterAsync(CreateChapterDto dto, int userId)
        {
            // Validate SubjectId exists
            var subject = await _subjectRepository.GetByIdAsync(dto.SubjectId);
            if (subject == null)
            {
                return (false, "Subject not found.", null);
            }

            // Check for duplicate ChapterNumber within the same Subject
            if (await _chapterRepository.ChapterNumberExistsAsync(dto.SubjectId, dto.ChapterNumber))
            {
                return (false, $"Chapter number {dto.ChapterNumber} already exists for this subject.", null);
            }

            var chapter = new Chapter
            {
                ChapterNumber = dto.ChapterNumber,
                ChapterTitle = dto.ChapterTitle,
                Description = dto.Description,
                SubjectId = dto.SubjectId,
                CreatedDate = DateTime.UtcNow,
                CreatedByUserId = userId,
                IsDeleted = false
            };

            var created = await _chapterRepository.CreateAsync(chapter);
            return (true, "Chapter created successfully.", MapToDto(created));
        }

        public async Task<(bool Success, string Message, ChapterDto? Chapter)> UpdateChapterAsync(UpdateChapterDto dto, int userId)
        {
            var existing = await _chapterRepository.GetByIdAsync(dto.Id);
            if (existing == null)
            {
                return (false, "Chapter not found.", null);
            }

            // Validate SubjectId exists
            var subject = await _subjectRepository.GetByIdAsync(dto.SubjectId);
            if (subject == null)
            {
                return (false, "Subject not found.", null);
            }

            // Check for duplicate ChapterNumber (excluding current chapter)
            if (await _chapterRepository.ChapterNumberExistsAsync(dto.SubjectId, dto.ChapterNumber, dto.Id))
            {
                return (false, $"Chapter number {dto.ChapterNumber} already exists for this subject.", null);
            }

            existing.ChapterNumber = dto.ChapterNumber;
            existing.ChapterTitle = dto.ChapterTitle;
            existing.Description = dto.Description;
            existing.SubjectId = dto.SubjectId;
            existing.UpdatedDate = DateTime.UtcNow;
            existing.UpdatedByUserId = userId;

            var updated = await _chapterRepository.UpdateAsync(existing);
            return (true, "Chapter updated successfully.", MapToDto(updated));
        }

        public async Task<(bool Success, string Message)> SoftDeleteChapterAsync(int id)
        {
            var chapter = await _chapterRepository.GetByIdAsync(id);
            if (chapter == null)
            {
                return (false, "Chapter not found.");
            }

            if (chapter.IsDeleted)
            {
                return (false, "Chapter is already deleted.");
            }

            await _chapterRepository.SoftDeleteAsync(id);
            return (true, "Chapter deleted successfully.");
        }

        public async Task<(bool Success, string Message)> RestoreChapterAsync(int id)
        {
            var chapter = await _chapterRepository.GetByIdAsync(id, includeDeleted: true);
            if (chapter == null)
            {
                return (false, "Chapter not found.");
            }

            if (!chapter.IsDeleted)
            {
                return (false, "Chapter is not deleted.");
            }

            // Kiểm tra trùng lặp số chương trong môn học
            if (await _chapterRepository.ChapterNumberExistsAsync(chapter.SubjectId, chapter.ChapterNumber))
            {
                return (false, $"Không thể khôi phục: Chương số {chapter.ChapterNumber} đã tồn tại trong các chương đang hoạt động của môn học này.");
            }

            await _chapterRepository.RestoreAsync(id);
            return (true, "Chapter restored successfully.");
        }

        /// <summary>
        /// Map Chapter entity to ChapterDto
        /// </summary>
        private ChapterDto MapToDto(Chapter chapter)
        {
            return new ChapterDto
            {
                Id = chapter.Id,
                ChapterNumber = chapter.ChapterNumber,
                ChapterTitle = chapter.ChapterTitle,
                Description = chapter.Description,
                SubjectId = chapter.SubjectId,
                CreatedDate = chapter.CreatedDate,
                CreatedByUsername = chapter.CreatedBy?.Username,
                UpdatedDate = chapter.UpdatedDate,
                UpdatedByUsername = chapter.UpdatedBy?.Username,
                IsDeleted = chapter.IsDeleted
            };
        }

        /// <summary>
        /// Map Chapter entity with Subject navigation property to ChapterDto
        /// </summary>
        private ChapterDto MapToDtoWithSubject(Chapter chapter)
        {
            var dto = MapToDto(chapter);
            if (chapter.Subject != null)
            {
                dto.SubjectCode = chapter.Subject.SubjectCode;
                dto.SubjectName = chapter.Subject.SubjectName;
            }
            return dto;
        }
    }
}
