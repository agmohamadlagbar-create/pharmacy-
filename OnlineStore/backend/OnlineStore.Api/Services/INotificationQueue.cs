using System.Threading;
using System.Threading.Tasks;

namespace OnlineStore.Api.Services;

public enum NotificationTaskType
{
    StoreOrderStatusUpdate,
    DeliveryAcceptJob,
    DeliveryStatusUpdate
}

public record NotificationWorkItem(
    NotificationTaskType TaskType,
    int OrderId,
    string? OrderGroupId,
    string? UserId,
    string? Status,
    int? JobId,
    string? CancellationReason = null,
    string? CancelledByRole = null
);

public interface INotificationQueue
{
    ValueTask QueueNotificationAsync(NotificationWorkItem item);
    ValueTask<NotificationWorkItem> DequeueAsync(CancellationToken cancellationToken);
}
