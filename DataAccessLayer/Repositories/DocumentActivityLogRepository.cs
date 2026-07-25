using DataAccessLayer.IRepositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.DbContexts;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    public class DocumentActivityLogRepository : IDocumentActivityLogRepository
    {
        private readonly AppDbContext _context;

        public DocumentActivityLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(DocumentActivityLog log)
        {
            await _context.DocumentActivityLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<DocumentActivityLog>> GetBySubjectIdAsync(int subjectId)
        {
            return await _context.DocumentActivityLogs
                .Include(l => l.User)
                .Where(l => l.SubjectId == subjectId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
    }
}
