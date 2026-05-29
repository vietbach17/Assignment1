using DataAccessLayer.DbContexts;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    /// <summary>
    /// Repository implementation cho Chapter entity sử dụng Entity Framework Core
    /// </summary>
    public class ChapterRepository : IChapterRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Constructor với Dependency Injection của AppDbContext
        /// </summary>
        public ChapterRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy tất cả các Chapter từ database, có thể bao gồm Subject thông qua eager loading
        /// </summary>
        public async Task<IEnumerable<Chapter>> GetAllAsync(bool includeDeleted = false)
        {
            IQueryable<Chapter> query = _context.Chapters.Include(c => c.Subject);
            
            if (!includeDeleted)
            {
                query = query.Where(c => !c.IsDeleted);
            }

            return await query
                .OrderBy(c => c.SubjectId)
                .ThenBy(c => c.ChapterNumber)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy tất cả các Chapter thuộc một Subject cụ thể, được sắp xếp theo ChapterNumber
        /// </summary>
        public async Task<IEnumerable<Chapter>> GetBySubjectIdAsync(int subjectId, bool includeDeleted = false)
        {
            var query = _context.Chapters.Where(c => c.SubjectId == subjectId);
            
            if (!includeDeleted)
            {
                query = query.Where(c => !c.IsDeleted);
            }

            return await query
                .OrderBy(c => c.ChapterNumber)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy một Chapter theo ID, không load navigation properties
        /// </summary>
        public async Task<Chapter?> GetByIdAsync(int id, bool includeDeleted = false)
        {
            var query = _context.Chapters.AsQueryable();
            
            if (!includeDeleted)
            {
                query = query.Where(c => !c.IsDeleted);
            }

            return await query.FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>
        /// Lấy một Chapter theo ID kèm theo thông tin Subject thông qua eager loading
        /// </summary>
        public async Task<Chapter?> GetByIdWithSubjectAsync(int id, bool includeDeleted = false)
        {
            IQueryable<Chapter> query = _context.Chapters.Include(c => c.Subject);
            
            if (!includeDeleted)
            {
                query = query.Where(c => !c.IsDeleted);
            }

            return await query.FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>
        /// Kiểm tra xem ChapterNumber đã tồn tại trong một Subject hay chưa
        /// Dùng để validate unique constraint (SubjectId, ChapterNumber)
        /// </summary>
        public async Task<bool> ChapterNumberExistsAsync(int subjectId, int chapterNumber, int? excludeId = null)
        {
            return await _context.Chapters
                .AnyAsync(c => c.SubjectId == subjectId && 
                              c.ChapterNumber == chapterNumber &&
                              !c.IsDeleted &&
                              (excludeId == null || c.Id != excludeId));
        }

        /// <summary>
        /// Tạo mới một Chapter trong database
        /// </summary>
        public async Task<Chapter> CreateAsync(Chapter chapter)
        {
            _context.Chapters.Add(chapter);
            await _context.SaveChangesAsync();
            return chapter;
        }

        /// <summary>
        /// Cập nhật thông tin một Chapter trong database
        /// </summary>
        public async Task<Chapter> UpdateAsync(Chapter chapter)
        {
            _context.Chapters.Update(chapter);
            await _context.SaveChangesAsync();
            return chapter;
        }

        /// <summary>
        /// Xóa mềm một Chapter bằng cách đánh dấu IsDeleted = true
        /// Giữ lại dữ liệu trong database để phục vụ audit trail
        /// </summary>
        public async Task<bool> SoftDeleteAsync(int id)
        {
            var chapter = await _context.Chapters.FindAsync(id);
            if (chapter == null || chapter.IsDeleted) return false;

            chapter.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Khôi phục một Chapter đã bị xóa mềm bằng cách đánh dấu IsDeleted = false
        /// </summary>
        public async Task<bool> RestoreAsync(int id)
        {
            var chapter = await _context.Chapters.FindAsync(id);
            if (chapter == null || !chapter.IsDeleted) return false;

            chapter.IsDeleted = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
