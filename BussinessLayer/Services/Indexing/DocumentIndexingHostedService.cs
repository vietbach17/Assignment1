using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataAccessLayer.IRepositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DocumentStatusEntity = DataAccessLayer.Models.DocumentStatus;

namespace BussinessLayer.Services.Indexing
{
    /// <summary>
    /// Worker nền: đọc hàng đợi index và xử lý từng tài liệu trong scope riêng.
    /// Khi khởi động, tự nạp lại các tài liệu còn kẹt ở trạng thái Pending
    /// (vd app tắt giữa chừng) để index tiếp.
    /// </summary>
    public class DocumentIndexingHostedService : BackgroundService
    {
        private readonly IDocumentIndexQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<DocumentIndexingHostedService> _logger;

        public DocumentIndexingHostedService(
            IDocumentIndexQueue queue,
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment env,
            ILogger<DocumentIndexingHostedService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _env = env;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RecoverPendingAsync(stoppingToken);

            await foreach (var request in _queue.DequeueAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var indexer = scope.ServiceProvider.GetRequiredService<IDocumentIndexer>();
                    await indexer.IndexAsync(request, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi index tài liệu {DocumentId}", request.DocumentId);
                }
            }
        }

        private async Task RecoverPendingAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                var all = await repo.GetAllAsync(includeDeleted: false);
                var wwwroot = _env.WebRootPath;

                foreach (var doc in all.Where(d => d.Status == DocumentStatusEntity.Pending))
                {
                    if (string.IsNullOrWhiteSpace(doc.FilePath)) continue;
                    var fullPath = Path.Combine(wwwroot, doc.FilePath.Replace('/', Path.DirectorySeparatorChar));
                    await _queue.EnqueueAsync(new DocumentIndexRequest(doc.Id, fullPath, doc.UploadedByUserId, wwwroot));
                }
            }
            catch (Exception ex)
            {
                // DB có thể chưa sẵn sàng lúc khởi động — không được làm sập host.
                _logger.LogWarning(ex, "Không thể nạp lại tài liệu Pending lúc khởi động.");
            }
        }
    }
}
