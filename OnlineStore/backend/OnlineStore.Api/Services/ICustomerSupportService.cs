using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services;

public interface ICustomerSupportService
{
    Task<OrderDetailsDto?> GetOrderDetailsAsync(string userId, int orderId);
    Task<OrderTrackingDto?> TrackOrderAsync(string userId, int orderId);
    Task<DeliveryEtaDto?> GetDeliveryEtaAsync(string userId, int orderId);
    Task<RefundStatusDto?> GetRefundStatusAsync(string userId, int orderId);
}
