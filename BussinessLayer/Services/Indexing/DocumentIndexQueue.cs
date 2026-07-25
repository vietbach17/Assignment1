using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BussinessLayer.Services.Indexing
{
    /// <summary>Một yêu cầu index (chunk + embed) cho một tài liệu.</summary>
    public record DocumentIndexRequest(int DocumentId, string FullFilePath, int UserId, string WwwrootPath);

    /// <summary>Hàng đợi nền để tách việc chunk/embed ra khỏi luồng request upload.</summary>
    public interface IDocumentIndexQueue
    {
        ValueTask EnqueueAsync(DocumentIndexRequest request);
        IAsyncEnumerable<DocumentIndexRequest> DequeueAllAsync(CancellationToken cancellationToken);
    }

    public class DocumentIndexQueue : IDocumentIndexQueue
    {
        private readonly Channel<DocumentIndexRequest> _channel =
            Channel.CreateUnbounded<DocumentIndexRequest>(new UnboundedChannelOptions { SingleReader = true });

        public ValueTask EnqueueAsync(DocumentIndexRequest request)
            => _channel.Writer.WriteAsync(request);

        public IAsyncEnumerable<DocumentIndexRequest> DequeueAllAsync(CancellationToken cancellationToken)
            => _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
