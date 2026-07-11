namespace OnlineStore.Api.Contracts;

/// <summary>
/// Structured Intent extracted by the LLM from the user's message.
/// The LLM does NOT execute tools; it only generates this object.
/// </summary>
public class ShoppingIntent
{
    /// <summary>
    /// "Search" | "AddToCart" | "UpdateQuantity" | "RemoveFromCart" | "GetCart" | "GeneralChat"
    /// </summary>
    public string Intent { get; set; } = "GeneralChat";

    /// <summary>
    /// Terms referring to a product (e.g. "first option", "cheapest", "apple", "it").
    /// </summary>
    public string? ProductReference { get; set; }

    /// <summary>
    /// Quantity extracted (e.g. 2, 3). Null if unspecified.
    /// </summary>
    public int? Quantity { get; set; }
}
