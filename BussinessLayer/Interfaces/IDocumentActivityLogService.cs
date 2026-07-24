using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.Interfaces
{
    public interface IDocumentActivityLogService
    {
        Task LogActivityAsync(int subjectId, int? documentId, string documentTitle, int userId, string action);
        Task<IEnumerable<DocumentActivityLogDto>> GetLogsBySubjectIdAsync(int subjectId);
    }
}
