using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace OnlineStore.Api.Services;

public class NotificationQueue : INotificationQueue
{
    private readonly Channel<NotificationWorkItem> _queue;

    public NotificationQueue(int capacity = 500)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        };
        _queue = Channel.CreateBounded<NotificationWorkItem>(options);
    }

    public async ValueTask QueueNotificationAsync(NotificationWorkItem item)
    {
        await _queue.Writer.WriteAsync(item);
    }

    public async ValueTask<NotificationWorkItem> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
