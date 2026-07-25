using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using DataAccessLayer.IRepositories;
using DocumentStatusEntity = DataAccessLayer.Models.DocumentStatus;

namespace BussinessLayer.Services.Indexing
{
    public interface IDocumentIndexer
    {
        Task IndexAsync(DocumentIndexRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Thực hiện toàn bộ pipeline index cho 1 tài liệu:
    /// trích xuất → chunk → embed TẤT CẢ chunk → lưu chunks_*.json → cập nhật trạng thái.
    /// </summary>
    public class DocumentIndexer : IDocumentIndexer
    {
        private readonly IGeminiService _gemini;
        private readonly IChunkSettingsService _chunkSettings;
        private readonly IDocumentRepository _documentRepository;

        public DocumentIndexer(
            IGeminiService gemini,
            IChunkSettingsService chunkSettings,
            IDocumentRepository documentRepository)
        {
            _gemini = gemini;
            _chunkSettings = chunkSettings;
            _documentRepository = documentRepository;
        }

        public async Task IndexAsync(DocumentIndexRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(request.FullFilePath))
                {
                    await _documentRepository.UpdateStatusAsync(request.DocumentId, DocumentStatusEntity.Failed);
                    return;
                }

                var settings = _chunkSettings.GetSettings();

                // 1. Chunk (kèm metadata trang), CHƯA embed.
                var chunks = (await _gemini.BuildChunksAsync(
                    request.FullFilePath, settings.MaxWords, settings.OverlapWords)).ToList();

                if (chunks.Count == 0)
                {
                    // Không rút được text (vd PDF scan) → đánh dấu Failed để phân biệt.
                    await _documentRepository.UpdateStatusAsync(request.DocumentId, DocumentStatusEntity.Failed);
                    return;
                }

                // 2. Embed TẤT CẢ chunk (theo lô).
                var embeddedCount = await _gemini.EmbedChunksAsync(chunks);

                // 3. Lưu ra file JSON cạnh tài liệu.
                var document = await _documentRepository.GetByIdAsync(request.DocumentId, false);
                if (document == null) return;

                var uploadsDir = Path.Combine(request.WwwrootPath, "uploads");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
                var savePath = Path.Combine(uploadsDir, $"chunks_{document.StoredFileName}.json");

                var payload = new
                {
                    documentId = document.Id,
                    savedAt = DateTime.UtcNow,
                    savedBy = request.UserId,
                    embeddingModel = "gemini-embedding-001",
                    dim = chunks.FirstOrDefault(c => c.Embedding != null)?.Embedding?.Length ?? 0,
                    chunkCount = chunks.Count,
                    embeddedCount,
                    // Trường tương thích UI cũ:
                    chunks = chunks.Select(c => c.Text).ToList(),
                    embedding = chunks.FirstOrDefault()?.Embedding?.ToList(),
                    // Trường mới cho retrieval:
                    chunkData = chunks
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(savePath, json, cancellationToken);

                await _documentRepository.UpdateStatusAsync(document.Id, DocumentStatusEntity.Indexed);
            }
            catch (Exception)
            {
                try { await _documentRepository.UpdateStatusAsync(request.DocumentId, DocumentStatusEntity.Failed); }
                catch { /* nuốt lỗi để không làm sập worker */ }
            }
        }
    }
}
