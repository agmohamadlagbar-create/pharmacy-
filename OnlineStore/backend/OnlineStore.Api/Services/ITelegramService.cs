using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services;

public interface ITelegramService
{
    // Connection
    Task<TelegramConnectionStatusDto> GetConnectionStatusAsync(string storeUserId);
    Task<TelegramConnectionStatusDto> ConnectAsync(string storeUserId, long chatId);
    Task DisconnectAsync(string storeUserId);

    // Notification settings
    Task<List<TelegramNotificationSettingDto>> GetNotificationSettingsAsync(string storeUserId);
    Task UpdateNotificationSettingsAsync(string storeUserId, List<TelegramNotificationSettingDto> settings);

    // Messaging
    Task SendMessageAsync(long chatId, string message);
    Task<bool> SendTestMessageAsync(string storeUserId);

    // Event-driven notifications (called from controllers)
    Task TrySendEventNotificationAsync(string storeUserId, string notificationType, string message);
}
