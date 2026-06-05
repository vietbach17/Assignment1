using DataAccessLayer.DbContexts;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    /// <summary>
    /// Repository xử lý thao tác dữ liệu cho Document
    /// Kế thừa pattern từ ChapterRepository: async/await + EF Core + eager loading
    /// </summary>
    public class DocumentRepository : IDocumentRepository
    {
        private readonly AppDbContext _context;

        public DocumentRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy tất cả tài liệu, eager load Subject, Chapter, UploadedBy
        /// </summary>
        public async Task<IEnumerable<Document>> GetAllAsync(bool includeDeleted = false)
        {
            var query = _context.Documents
                .Include(d => d.Subject)
                .Include(d => d.Chapter)
                .Include(d => d.UploadedBy)
                .AsQueryable();

            if (!includeDeleted)
                query = query.Where(d => !d.IsDeleted);

            return await query
                .OrderByDescending(d => d.UploadedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy tài liệu theo Id, kèm navigation properties
        /// </summary>
        public async Task<Document?> GetByIdAsync(int id, bool includeDeleted = false)
        {
            var query = _context.Documents
                .Include(d => d.Subject)
                .Include(d => d.Chapter)
                .Include(d => d.UploadedBy)
                .AsQueryable();

            if (!includeDeleted)
                query = query.Where(d => !d.IsDeleted);

            return await query.FirstOrDefaultAsync(d => d.Id == id);
        }

        /// <summary>
        /// Lấy danh sách tài liệu theo SubjectId
        /// </summary>
        public async Task<IEnumerable<Document>> GetBySubjectIdAsync(int subjectId, bool includeDeleted = false)
        {
            var query = _context.Documents
                .Include(d => d.Subject)
                .Include(d => d.Chapter)
                .Include(d => d.UploadedBy)
                .Where(d => d.SubjectId == subjectId);

            if (!includeDeleted)
                query = query.Where(d => !d.IsDeleted);

            return await query
                .OrderByDescending(d => d.UploadedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy danh sách tài liệu theo ChapterId
        /// </summary>
        public async Task<IEnumerable<Document>> GetByChapterIdAsync(int chapterId, bool includeDeleted = false)
        {
            var query = _context.Documents
                .Include(d => d.Subject)
                .Include(d => d.Chapter)
                .Include(d => d.UploadedBy)
                .Where(d => d.ChapterId == chapterId);

            if (!includeDeleted)
                query = query.Where(d => !d.IsDeleted);

            return await query
                .OrderByDescending(d => d.UploadedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Document>> GetByUploadedByUserIdAsync(int uploadedByUserId, bool includeDeleted = false)
        {
            var query = _context.Documents
                .Include(d => d.Subject)
                .Include(d => d.Chapter)
                .Include(d => d.UploadedBy)
                .Where(d => d.UploadedByUserId == uploadedByUserId);

            if (!includeDeleted)
                query = query.Where(d => !d.IsDeleted);

            return await query
                .OrderByDescending(d => d.UploadedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Thêm tài liệu mới vào database
        /// </summary>
        public async Task<Document> AddAsync(Document document)
        {
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();
            return document;
        }

        /// <summary>
        /// Cập nhật thông tin tài liệu
        /// </summary>
        public async Task<Document> UpdateAsync(Document document)
        {
            _context.Documents.Update(document);
            await _context.SaveChangesAsync();
            return document;
        }

        /// <summary>
        /// Xoá mềm tài liệu: set IsDeleted = true, KHÔNG xoá file vật lý (phần đó do Service xử lý)
        /// </summary>
        public async Task<bool> SoftDeleteAsync(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
                return false;

            document.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Cập nhật trạng thái xử lý của tài liệu: Pending / Indexed / Failed
        /// </summary>
        public async Task<bool> UpdateStatusAsync(int id, DocumentStatus status)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
                return false;

            document.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Tìm tài liệu theo mã băm SHA-256 nội dung file
        /// </summary>
        public async Task<Document?> GetByHashAsync(string fileHash)
        {
            return await _context.Documents
                .Include(d => d.Subject)
                .Include(d => d.Chapter)
                .Include(d => d.UploadedBy)
                .FirstOrDefaultAsync(d => d.FileHash == fileHash && !d.IsDeleted);
        }
    }
}
