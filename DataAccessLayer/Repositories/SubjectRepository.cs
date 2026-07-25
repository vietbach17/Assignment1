using DataAccessLayer.IRepositories;
using DataAccessLayer.DbContexts;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    /// <summary>
    /// Triển khai Repository tương tác DB thực tế cho Subject bằng EF Core
    /// </summary>
    public class SubjectRepository : ISubjectRepository
    {
        private readonly AppDbContext _context;

        // Dependency Injection tiêm DbContext vào Repository
        public SubjectRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Subject>> GetAllAsync(bool includeDeleted = false)
        {
            var query = _context.Subjects
                .Include(s => s.SubjectLecturers)
                .ThenInclude(sl => sl.Lecturer)
                .AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(s => !s.IsDeleted);
            }

            return await query
                .OrderBy(s => s.SubjectCode)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subject>> GetByLecturerIdAsync(int lecturerId, bool includeDeleted = false)
        {
            var query = _context.Subjects
                .Include(s => s.SubjectLecturers)
                .ThenInclude(sl => sl.Lecturer)
                .Where(s => s.SubjectLecturers.Any(sl => sl.LecturerId == lecturerId));

            if (!includeDeleted)
            {
                query = query.Where(s => !s.IsDeleted);
            }

            return await query
                .OrderBy(s => s.SubjectCode)
                .ToListAsync();
        }

        public async Task<Subject?> GetByIdAsync(int id, bool includeDeleted = false)
        {
            var query = _context.Subjects
                .Include(s => s.SubjectLecturers).ThenInclude(sl => sl.Lecturer)
                .AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(s => !s.IsDeleted);
            }

            return await query.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Subject?> GetByIdWithChaptersAsync(int id, bool includeDeleted = false)
        {
            IQueryable<Subject> query = _context.Subjects
                .Include(s => s.SubjectLecturers).ThenInclude(sl => sl.Lecturer)
                .Include(s => s.Chapters.Where(c => !c.IsDeleted).OrderBy(c => c.ChapterNumber));

            if (!includeDeleted)
            {
                query = query.Where(s => !s.IsDeleted);
            }

            return await query.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Subject?> GetBySubjectCodeAsync(string subjectCode, bool includeDeleted = false)
        {
            var query = _context.Subjects.AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(s => !s.IsDeleted);
            }

            return await query.FirstOrDefaultAsync(s => s.SubjectCode == subjectCode);
        }

        public async Task<bool> SubjectCodeExistsAsync(string subjectCode, int? excludeId = null)
        {
            return await _context.Subjects
                .AnyAsync(s => s.SubjectCode == subjectCode &&
                              !s.IsDeleted &&
                              (excludeId == null || s.Id != excludeId));
        }

        public async Task<bool> IsLecturerAssignedToSubjectAsync(int subjectId, int lecturerId)
        {
            return await _context.SubjectLecturers
                .AnyAsync(sl => sl.SubjectId == subjectId && sl.LecturerId == lecturerId);
        }

        public async Task<Subject> CreateAsync(Subject subject)
        {
            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();
            return subject;
        }

        public async Task<Subject> UpdateAsync(Subject subject)
        {
            _context.Subjects.Update(subject);
            await _context.SaveChangesAsync();
            return subject;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null || subject.IsDeleted) return false;

            subject.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreAsync(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null || !subject.IsDeleted) return false;

            subject.IsDeleted = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasChaptersAsync(int subjectId)
        {
            return await _context.Chapters.AnyAsync(c => c.SubjectId == subjectId && !c.IsDeleted);
        }

        public async Task AssignLecturerAsync(int subjectId, int lecturerId)
        {
            var assignment = new SubjectLecturer
            {
                SubjectId = subjectId,
                LecturerId = lecturerId,
                AssignedDate = DateTime.UtcNow
            };

            _context.SubjectLecturers.Add(assignment);
            await _context.SaveChangesAsync();
        }

        public async Task UnassignLecturerAsync(int subjectId, int lecturerId)
        {
            var assignment = await _context.SubjectLecturers
                .FirstOrDefaultAsync(sl => sl.SubjectId == subjectId && sl.LecturerId == lecturerId);

            if (assignment != null)
            {
                _context.SubjectLecturers.Remove(assignment);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<User>> GetAssignedLecturersAsync(int subjectId)
        {
            return await _context.SubjectLecturers
                .Where(sl => sl.SubjectId == subjectId)
                .Include(sl => sl.Lecturer)
                .ThenInclude(l => l.Role)
                .Select(sl => sl.Lecturer)
                .ToListAsync();
        }
    }
}
