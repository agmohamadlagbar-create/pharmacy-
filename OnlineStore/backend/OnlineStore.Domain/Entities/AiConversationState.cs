namespace OnlineStore.Domain.Entities;

public class AiConversationState
{
    public int? PendingProductId { get; set; }
    public string? PendingProductName { get; set; }
    public int PendingQuantity { get; set; }
    public string? PendingAction { get; set; }
    public bool AwaitingConfirmation { get; set; }
}
