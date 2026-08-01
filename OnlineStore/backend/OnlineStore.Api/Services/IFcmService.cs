namespace OnlineStore.Api.Services;

public interface IFcmService
{
    Task RegisterDeviceTokenAsync(string userId, string token, string platform);
    Task UnregisterDeviceTokenAsync(string userId, string token);
    Task SendPushNotificationToUserAsync(string userId, string title, string message, IDictionary<string, string>? data = null);
}
