namespace OnlineStore.Domain.Entities;

public class AiConversationState
{
    public int? PendingProductId { get; set; }
    public string? PendingProductName { get; set; }
    public int PendingQuantity { get; set; }
    public string? PendingAction { get; set; }
    public bool AwaitingConfirmation { get; set; }

    public string? PendingIntentJson { get; set; }
    public string? LastShoppingIntentJson { get; set; }
    public int? SelectedProductId { get; set; }
    public string? SelectedStoreName { get; set; }
    public string? LastSuccessfulToolResult { get; set; }
}
