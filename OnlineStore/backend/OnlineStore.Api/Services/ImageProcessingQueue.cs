using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace OnlineStore.Api.Services;

public record ImageProcessingItem(
    string StorageKey,
    string EntityType, // "products", "users", "ads", "categories"
    string EntityId,
    string Country,
    string City,
    string OriginalFileName
);

public interface IImageProcessingQueue
{
    ValueTask QueueImageWorkAsync(ImageProcessingItem item);
    ValueTask<ImageProcessingItem> DequeueAsync(CancellationToken cancellationToken);
}

public class ImageProcessingQueue : IImageProcessingQueue
{
    private readonly Channel<ImageProcessingItem> _queue;

    public ImageProcessingQueue(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<ImageProcessingItem>(options);
    }

    public async ValueTask QueueImageWorkAsync(ImageProcessingItem item)
    {
        await _queue.Writer.WriteAsync(item);
    }

    public async ValueTask<ImageProcessingItem> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
