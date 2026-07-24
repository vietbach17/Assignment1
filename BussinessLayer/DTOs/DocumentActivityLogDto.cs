using System;

namespace BussinessLayer.DTOs
{
    public class DocumentActivityLogDto
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public int? DocumentId { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
