using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories
{
    public interface IDocumentActivityLogRepository
    {
        Task AddAsync(DocumentActivityLog log);
        Task<IEnumerable<DocumentActivityLog>> GetBySubjectIdAsync(int subjectId);
    }
}
