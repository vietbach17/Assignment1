using BussinessLayer.IServices;
using DataAccessLayer.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;

namespace BussinessLayer.Services
{
    public class DocumentActivityLogService : IDocumentActivityLogService
    {
        private readonly IDocumentActivityLogRepository _repository;

        public DocumentActivityLogService(IDocumentActivityLogRepository repository)
        {
            _repository = repository;
        }

        public async Task LogActivityAsync(int subjectId, int? documentId, string documentTitle, int userId, string action)
        {
            var log = new DocumentActivityLog
            {
                SubjectId = subjectId,
                DocumentId = documentId,
                DocumentTitle = documentTitle,
                UserId = userId,
                Action = action,
                Timestamp = DateTime.UtcNow
            };

            await _repository.AddAsync(log);
        }

        public async Task<IEnumerable<DocumentActivityLogDto>> GetLogsBySubjectIdAsync(int subjectId)
        {
            var logs = await _repository.GetBySubjectIdAsync(subjectId);
            return logs.Select(l => new DocumentActivityLogDto
            {
                Id = l.Id,
                SubjectId = l.SubjectId,
                DocumentId = l.DocumentId,
                DocumentTitle = l.DocumentTitle,
                UserId = l.UserId,
                Username = l.User?.Username ?? "Unknown",
                Action = l.Action,
                Timestamp = l.Timestamp
            });
        }
    }
}
