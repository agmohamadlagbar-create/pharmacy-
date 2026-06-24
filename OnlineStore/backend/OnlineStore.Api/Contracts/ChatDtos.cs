using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OnlineStore.Api.Contracts;

/// <summary>
/// Represents a request sent to the AI chat endpoint.
/// </summary>
public class ChatRequestDto
{
    /// <summary>
    /// User-provided message.
    /// </summary>
    [Required(ErrorMessage = "Message is required.")]
    [StringLength(2000, ErrorMessage = "Message cannot exceed 2000 characters.")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional conversation identifier to continue an existing thread.
    /// </summary>
    public int? ConversationId { get; set; }

    /// <summary>
    /// Optional list of tool calls that the model may invoke.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ToolCallDto>? ToolCalls { get; set; }
}

/// <summary>
/// Represents the response returned from the AI chat endpoint.
/// </summary>
public class ChatResponseDto
{
    /// <summary>
    /// The AI generated response text.
    /// </summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the conversation that this response belongs to.
    /// </summary>
    public int ConversationId { get; set; }

    /// <summary>
    /// Any tool calls the model decided to make.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ToolCallDto>? ToolCalls { get; set; }

    /// <summary>
    /// Optional product results that the model recommends.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ProductResultDto>? Products { get; set; }
}

/// <summary>
/// Represents a call to a backend tool/function that the AI wants to execute.
/// </summary>
public class ToolCallDto
{
    /// <summary>
    /// Unique identifier for the tool call.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Name of the function/tool to invoke.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// JSON-encoded arguments for the tool.
    /// </summary>
    [Required]
    public string Arguments { get; set; } = string.Empty;
}

/// <summary>
/// DTO for a product that the AI recommends or references.
/// </summary>
public class ProductResultDto
{
    /// <summary>
    /// Unique product identifier.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Human‑readable product name.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Price in the store's currency.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Price must be non‑negative.")]
    public decimal Price { get; set; }

    /// <summary>
    /// Current stock level.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Stock must be non‑negative.")]
    public int Stock { get; set; }

    /// <summary>
    /// Optional description.
    /// </summary>
    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// URL to the product image.
    /// </summary>
    [Url]
    public string? ImageUrl { get; set; }
}

/// <summary>
/// Represents a cart operation requested by the AI or UI.
/// </summary>
public class CartActionDto
{
    /// <summary>
    /// Type of action to perform on the cart.
    /// </summary>
    [Required]
    public CartActionType Action { get; set; }

    /// <summary>
    /// Target product identifier.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Quantity for add / update actions.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// Allowed cart actions.
/// </summary>
public enum CartActionType
{
    Add,
    Remove,
    Update,
    Checkout
}
