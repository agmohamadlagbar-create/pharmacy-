using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OnlineStore.Api.Contracts;
using OnlineStore.Infrastructure.Persistence;
using OnlineStore.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace OnlineStore.Api.Services;

public class GroqAiService : IGroqAiService
{
    private readonly HttpClient _httpClient;
    private readonly IProductSearchService _productSearchService;
    private readonly ICartService _cartService;
    private readonly ICustomerSupportService _customerSupportService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly IStoreSelectionService _storeSelectionService;
    private readonly IRecommendationService _recommendationService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GroqAiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string ModelName = "llama-3.3-70b-versatile"; // Better quality, fewer hallucinations
    private const int MaxToolRounds = 4;
    private const int ConversationHistoryCount = 20;

    private const string SystemPrompt =
        "You are an AI ecommerce assistant and customer support agent.\n\n" +
        "IMPORTANT BEHAVIOR RULES:\n\n" +

        "1. NEVER hallucinate actions.\n" +
        "   - Never say a product was added to the cart unless the backend explicitly confirms it.\n" +
        "   - Never say checkout succeeded unless the backend confirms success.\n" +
        "   - Never say a product exists unless search results confirm it.\n" +
        "   - Never invent product availability, prices, delivery times, or stock.\n\n" +

        "2. The backend is the ONLY source of truth.\n" +
        "   - Conversation history is NOT the source of truth.\n" +
        "   - Memory is NOT the cart state.\n" +
        "   - Always trust API responses over assumptions.\n\n" +

        "3. BEFORE responding about cart actions:\n" +
        "   - Verify the latest backend response.\n" +
        "   - If the backend returns failure, explain the failure clearly.\n" +
        "   - If the backend returns success, confirm the action clearly.\n\n" +

        "4. STRICT RESPONSE RULES:\n" +
        "   If product search returns 0 results:\n" +
        "   - Say the product was not found.\n" +
        "   - DO NOT suggest it was added successfully.\n" +
        "   If add-to-cart fails:\n" +
        "   - Clearly explain that the item could not be added.\n" +
        "   - DO NOT pretend the action succeeded.\n" +
        "   If nearby stores are unavailable:\n" +
        "   - Explain that the product may be out of stock or unavailable in the user's area.\n\n" +

        "5. MULTI-LANGUAGE SUPPORT:\n" +
        "   - Detect the customer's language automatically.\n" +
        "   - Respond in the same language as the customer.\n" +
        "   - Backend logic must remain identical in all languages.\n" +
        "   - Never skip backend validation because the user speaks another language.\n\n" +

        "6. TOOL/API VALIDATION RULES:\n" +
        "   Only confirm actions if: success == true OR status == \"success\".\n" +
        "   Treat all other responses as failure.\n\n" +

        "7. CART STATE RULES:\n" +
        "   - Always fetch the latest cart state before discussing the cart.\n" +
        "   - Never assume previous cart actions succeeded.\n" +
        "   - Never rely on previous assistant messages as proof.\n\n" +

        "8. ERROR HANDLING:\n" +
        "   If backend responses are unclear or malformed:\n" +
        "   - Say: \"I could not verify the backend response. Please try again.\"\n" +
        "   - Do NOT guess the outcome.\n\n" +

        "9. PRODUCT SEARCH RULES:\n" +
        "   - If multiple products are found, ask the user which one they want.\n" +
        "   - If the search query is vague, ask clarifying questions.\n" +
        "   - Never automatically add products after only a search request.\n\n" +

        "10. VERY IMPORTANT:\n" +
        "    Searching for a product DOES NOT mean adding it to the cart.\n" +
        "    Only add items when the user explicitly says: 'add to cart', 'buy this', 'add this item', or equivalent.\n\n" +

        "11. RESPONSE PRIORITY:\n" +
        "    Backend/API result > system rules > conversation history > model assumptions.\n\n" +

        "12. NEVER SAY:\n" +
        "    - 'Added successfully' without backend confirmation.\n" +
        "    - 'Order completed' without backend confirmation.\n" +
        "    - 'Product found nearby' without backend proof.\n" +
        "    - Any statement that assumes backend success.\n\n" +

        "13. NEVER output a plain text list of products, prices, or store details when calling get_recommendations.\n" +
        "    - The frontend will automatically render a premium carousel of interactive product cards for the customer.\n" +
        "    - Instead of writing out the list, write a very brief, friendly conversational introduction (e.g., 'I have loaded some personalized recommendations for you below! 🛒') and ask if they would like to add any to their cart or have any questions.\n\n" +

        "Always behave like a reliable production ecommerce assistant, not a guessing chatbot.";

    public GroqAiService(
        HttpClient httpClient,
        IProductSearchService productSearchService,
        ICartService cartService,
        ICustomerSupportService customerSupportService,
        IChatHistoryService chatHistoryService,
        IStoreSelectionService storeSelectionService,
        IRecommendationService recommendationService,
        ApplicationDbContext db,
        ILogger<GroqAiService> logger)
    {
        _httpClient = httpClient;
        _productSearchService = productSearchService;
        _cartService = cartService;
        _customerSupportService = customerSupportService;
        _chatHistoryService = chatHistoryService;
        _storeSelectionService = storeSelectionService;
        _recommendationService = recommendationService;
        _db = db;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<string> AskAsync(string userMessage, string userId, int conversationId)
    {
        return await ProcessConversationAsync(
            userMessage,
            userId,
            conversationId,
            onToken: null,
            cancellationToken: CancellationToken.None);
    }

    public async Task<string> AskStreamingAsync(
        string userMessage,
        string userId,
        int conversationId,
        Func<string, CancellationToken, Task> onToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onToken);

        return await ProcessConversationAsync(
            userMessage,
            userId,
            conversationId,
            onToken,
            cancellationToken);
    }

    private async Task<string> ExecuteToolAsync(GroqToolCallDto toolCall, string userId, int conversationId)
    {
        _logger.LogDebug("Executing tool {ToolName} with arguments: {Arguments} for user {UserId}",
            toolCall.Function.Name, toolCall.Function.Arguments, userId);

        switch (toolCall.Function.Name)
        {
            case "search_products":
            {
                var args = JsonSerializer.Deserialize<SearchProductsArgs>(toolCall.Function.Arguments);
                var productName = args?.ProductName ?? "";
                
                double userLat = 0, userLon = 0;
                var user = await _db.Users.Cast<AppUser>().FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    userLat = user.Latitude ?? 0;
                    userLon = user.Longitude ?? 0;
                }

                _logger.LogInformation("Searching products: Name={ProductName}, Quantity={Quantity}, UserLoc={Lat},{Lon}", 
                    productName, args?.Quantity ?? 0, userLat, userLon);
                
                var searchResult = await _productSearchService.SearchProductsAsync(productName, userLat, userLon);
                if (args != null && args.Quantity > 0)
                {
                    searchResult.AllAvailable = searchResult.AllAvailable
                        .Where(p => p.Stock >= args.Quantity)
                        .ToList();
                }
                
                var topProduct = searchResult.AllAvailable.FirstOrDefault();
                if (topProduct != null)
                {
                    var conversation = await _db.AiConversations.FindAsync(conversationId);
                    if (conversation != null)
                    {
                        conversation.State.PendingProductId = topProduct.ProductId;
                        conversation.State.PendingProductName = topProduct.ProductName;
                        conversation.State.PendingQuantity = args?.Quantity > 0 ? args.Quantity : 1;
                        conversation.State.PendingAction = "add_to_cart";
                        conversation.State.AwaitingConfirmation = true;
                        await _db.SaveChangesAsync();
                        _logger.LogInformation("Saved PendingProductId={ProductId} to conversation state.", topProduct.ProductId);
                    }
                }

                _logger.LogInformation("Product search returned {Count} results", searchResult.AllAvailable?.Count ?? 0);
                
                // Return sanitized result so AI doesn't see ProductId
                var sanitizedResult = new 
                {
                    Count = searchResult.AllAvailable.Count,
                    Products = searchResult.AllAvailable.Select(p => new {
                        p.ProductName,
                        p.Price,
                        p.Stock,
                        p.StoreName,
                        p.DistanceKm
                    })
                };
                return JsonSerializer.Serialize(sanitizedResult);
            }
            case "add_to_cart":
            {
                var args = JsonSerializer.Deserialize<CartActionArgs>(toolCall.Function.Arguments);
                _logger.LogInformation("add_to_cart: Parsed args => ProductId={ProductId}, Quantity={Quantity}",
                    args?.ProductId, args?.Quantity);

                int productIdToUse = args?.ProductId ?? 0;
                
                // If AI doesn't pass a ProductId or passes 0, fallback to state
                if (productIdToUse <= 0)
                {
                    var conversation = await _db.AiConversations.FindAsync(conversationId);
                    if (conversation?.State.PendingProductId != null)
                    {
                        productIdToUse = conversation.State.PendingProductId.Value;
                        _logger.LogInformation("Using PendingProductId={ProductId} from state for add_to_cart.", productIdToUse);
                    }
                }

                if (productIdToUse <= 0)
                {
                    return "Error: Could not identify which product to add. Please search for the product first.";
                }

                var qty = args?.Quantity > 0 ? args.Quantity : 1;
                var cartResult = await _cartService.AddToCartAsync(userId, productIdToUse, qty);
                _logger.LogInformation("add_to_cart result: {Result}", cartResult);
                
                // Clear state
                var conv = await _db.AiConversations.FindAsync(conversationId);
                if (conv != null)
                {
                    conv.State = new();
                    await _db.SaveChangesAsync();
                }

                return cartResult;
            }
            case "update_quantity":
            {
                var args = JsonSerializer.Deserialize<CartActionArgs>(toolCall.Function.Arguments);
                return args != null
                    ? await _cartService.UpdateQuantityAsync(userId, args.ProductId, args.Quantity)
                    : "Error: invalid arguments.";
            }
            case "remove_from_cart":
            {
                var args = JsonSerializer.Deserialize<CartActionArgs>(toolCall.Function.Arguments);
                return args != null
                    ? await _cartService.RemoveFromCartAsync(userId, args.ProductId)
                    : "Error: invalid arguments.";
            }
            case "track_order":
            {
                var args = JsonSerializer.Deserialize<OrderActionArgs>(toolCall.Function.Arguments);
                return args != null
                    ? JsonSerializer.Serialize(await _customerSupportService.TrackOrderAsync(userId, args.OrderId))
                    : "Error: invalid arguments.";
            }
            case "get_delivery_eta":
            {
                var args = JsonSerializer.Deserialize<OrderActionArgs>(toolCall.Function.Arguments);
                return args != null
                    ? JsonSerializer.Serialize(await _customerSupportService.GetDeliveryEtaAsync(userId, args.OrderId))
                    : "Error: invalid arguments.";
            }
            case "get_refund_status":
            {
                var args = JsonSerializer.Deserialize<OrderActionArgs>(toolCall.Function.Arguments);
                return args != null
                    ? JsonSerializer.Serialize(await _customerSupportService.GetRefundStatusAsync(userId, args.OrderId))
                    : "Error: invalid arguments.";
            }
            case "get_order_details":
            {
                var args = JsonSerializer.Deserialize<OrderActionArgs>(toolCall.Function.Arguments);
                return args != null
                    ? JsonSerializer.Serialize(await _customerSupportService.GetOrderDetailsAsync(userId, args.OrderId))
                    : "Error: invalid arguments.";
            }
            case "find_best_store":
            {
                var args = JsonSerializer.Deserialize<FindBestStoreArgs>(toolCall.Function.Arguments);
                return args != null
                    ? JsonSerializer.Serialize(await _storeSelectionService.FindBestStoreAsync(
                        args.ProductName, args.Quantity, args.CustomerLatitude, args.CustomerLongitude))
                    : "Error: invalid arguments.";
            }
            case "get_recommendations":
            {
                var args = JsonSerializer.Deserialize<RecommendationArgs>(toolCall.Function.Arguments);
                int? pid = args?.ProductId > 0 ? args.ProductId : null;
                
                try 
                {
                    var recommendations = await _recommendationService.GetRecommendationsAsync(userId, pid);
                    return JsonSerializer.Serialize(recommendations);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in GetRecommendationsAsync for user {UserId}", userId);
                    return "Error: Unable to fetch recommendations at this time.";
                }
            }
            case "get_cart":
            {
                return await _cartService.GetCartSummaryAsync(userId);
            }
            default:
                return $"Unknown tool: {toolCall.Function.Name}";
        }
    }

    private async Task<string> ProcessConversationAsync(
        string userMessage,
        string userId,
        int conversationId,
        Func<string, CancellationToken, Task>? onToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("User context is required.");

        if (string.IsNullOrWhiteSpace(userMessage))
            return string.Empty;

        var sw = Stopwatch.StartNew();
        _logger.LogInformation("AI Conversation Request Started: [User: {UserId}] [ConversationId: {ConversationId}]", userId, conversationId);

        await _chatHistoryService.SaveMessageAsync(userId, conversationId, "user", userMessage);

        var history = await _chatHistoryService.LoadRecentMessagesAsync(userId, conversationId, ConversationHistoryCount);
        var messages = new List<GroqMessageDto>
        {
            new GroqMessageDto { Role = "system", Content = SystemPrompt }
        };
        messages.AddRange(history);

        StringBuilder fullAssistantResponse = new StringBuilder();
        var maxToolRoundsReached = false;
        var executedToolCalls = new HashSet<string>();

        for (var round = 0; round < MaxToolRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var requestDto = new GroqChatRequestDto
            {
                Model = ModelName,
                Messages = messages,
                Tools = GetAvailableTools(),
                Stream = onToken is not null
            };

            var iterationResult = onToken is null
                ? await SendRequestAsync(requestDto, cancellationToken)
                : await SendStreamingRequestAsync(requestDto, onToken, cancellationToken);

            if (!string.IsNullOrEmpty(iterationResult.Content))
            {
                if (fullAssistantResponse.Length > 0) fullAssistantResponse.Append("\n\n");
                fullAssistantResponse.Append(iterationResult.Content);
            }

            if (!iterationResult.ToolCalls.Any())
            {
                maxToolRoundsReached = false;
                break;
            }

            maxToolRoundsReached = round == MaxToolRounds - 1;

            messages.Add(new GroqMessageDto
            {
                Role = "assistant",
                Content = string.IsNullOrWhiteSpace(iterationResult.Content) ? null : iterationResult.Content,
                ToolCalls = iterationResult.ToolCalls
            });

            foreach (var toolCall in iterationResult.ToolCalls)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Deduplicate: Don't execute the same tool with the same arguments in the same round
                var toolKey = $"{toolCall.Function.Name}:{toolCall.Function.Arguments}";
                if (executedToolCalls.Contains(toolKey))
                {
                    _logger.LogWarning("Skipping duplicate tool call in the same conversation turn: {ToolKey}", toolKey);
                    continue;
                }
                executedToolCalls.Add(toolKey);

                var toolExecution = await ExecuteAndPersistToolCallAsync(toolCall, userId, conversationId);

                // If cart was modified AND the backend confirmed success, notify frontend
                // Rule 6: Only treat as success if backend explicitly says success==true or status=="success"
                if (onToken != null &&
                    (toolCall.Function.Name == "add_to_cart" ||
                     toolCall.Function.Name == "update_quantity" ||
                     toolCall.Function.Name == "remove_from_cart") &&
                    !toolExecution.StartsWith("Error") &&
                    (toolExecution.Contains("\"success\":true", StringComparison.OrdinalIgnoreCase) ||
                     toolExecution.Contains("\"status\":\"success\"", StringComparison.OrdinalIgnoreCase) ||
                     toolExecution.Contains("success", StringComparison.OrdinalIgnoreCase)))
                {
                    await onToken("[SYSTEM_EVENT:cart_updated]", cancellationToken);
                }

                // If recommendations were fetched, emit structured data so the frontend can render rich product cards
                if (onToken != null &&
                    toolCall.Function.Name == "get_recommendations" &&
                    !toolExecution.StartsWith("Error"))
                {
                    await onToken($"[SYSTEM_EVENT:recommendations:{toolExecution}]", cancellationToken);
                }

                messages.Add(new GroqMessageDto
                {
                    Role = "tool",
                    ToolCallId = toolCall.Id,
                    Name = toolCall.Function.Name,
                    Content = toolExecution
                });
            }
        }

        string assistantResponse = fullAssistantResponse.ToString();
        
        // Final fallback to avoid empty bubbles
        if (string.IsNullOrWhiteSpace(assistantResponse))
        {
            assistantResponse = "I've processed your request. Is there anything else I can help you with?";
        }

        if (maxToolRoundsReached)
        {
            _logger.LogWarning(
                "AI conversation {ConversationId} for user {UserId} reached max tool rounds ({MaxToolRounds}).",
                conversationId,
                userId,
                MaxToolRounds);
        }

        if (!string.IsNullOrWhiteSpace(assistantResponse))
        {
            await _chatHistoryService.SaveMessageAsync(userId, conversationId, "assistant", assistantResponse);
        }

        sw.Stop();
        _logger.LogInformation("AI Conversation Request Completed: [User: {UserId}] [ConversationId: {ConversationId}] [Elapsed: {ElapsedMs}ms]", userId, conversationId, sw.ElapsedMilliseconds);

        return assistantResponse;
    }

    private async Task<string> ExecuteAndPersistToolCallAsync(GroqToolCallDto toolCall, string userId, int conversationId)
    {
        string toolResultStr = "Error executing tool.";
        var toolStatus = "Succeeded";
        string? toolError = null;
        var startedAtUtc = DateTime.UtcNow;
        var sw = Stopwatch.StartNew();

        _logger.LogInformation("AI Tool Call Started: [Name: {ToolName}] [Arguments: {Arguments}]", toolCall.Function.Name, toolCall.Function.Arguments);

        try
        {
            int maxRetries = 3;
            int retryCount = 0;
            TimeSpan delay = TimeSpan.FromSeconds(1);

            while (true)
            {
                try
                {
                    toolResultStr = await ExecuteToolAsync(toolCall, userId, conversationId);
                    break; // Success, break out of loop
                }
                catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning(httpEx, "Groq rate limit hit (attempt {Attempt}/{Max}). Waiting {Delay}s before retry.",
                        retryCount + 1, maxRetries, delay.TotalSeconds);

                    if (++retryCount > maxRetries)
                    {
                        toolResultStr = $"Error executing {toolCall.Function.Name}: The AI is currently experiencing high demand (Rate Limit Reached). Please try again in a few minutes.";
                        toolStatus = "Failed";
                        toolError = "Rate Limit Exceeded after retries.";
                        break;
                    }

                    await Task.Delay(delay);
                    delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error executing tool {ToolName}. ExceptionType: {ExceptionType}, Message: {ErrorMessage}, " +
                        "InnerException: {InnerMessage}, Arguments: {Arguments}",
                        toolCall.Function.Name,
                        ex.GetType().FullName,
                        ex.Message,
                        ex.InnerException?.Message ?? "(none)",
                        toolCall.Function.Arguments);
                    toolResultStr = $"Error executing {toolCall.Function.Name}: {ex.Message}";
                    toolStatus = "Failed";
                    toolError = $"{ex.GetType().Name}: {ex.Message}";
                    break;
                }
            }
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation("AI Tool Call Completed: [Name: {ToolName}] [Status: {Status}] [Elapsed: {ElapsedMs}ms]", toolCall.Function.Name, toolStatus, sw.ElapsedMilliseconds);
        }

        try
        {
            await _chatHistoryService.SaveToolCallAsync(
                userId: userId,
                conversationId: conversationId,
                messageId: null,
                toolCallId: toolCall.Id,
                toolName: toolCall.Function.Name,
                argumentsJson: toolCall.Function.Arguments,
                resultJson: toolResultStr,
                status: toolStatus,
                startedAtUtc: startedAtUtc,
                completedAtUtc: DateTime.UtcNow,
                errorMessage: toolError);
        }
        catch (Exception persistEx)
        {
            _logger.LogError(
                persistEx,
                "Failed to persist tool call {ToolCallId} for conversation {ConversationId}.",
                toolCall.Id,
                conversationId);
        }

        return toolResultStr;
    }

    private List<GroqToolDto> GetAvailableTools()
    {
        return new List<GroqToolDto>
        {
            new GroqToolDto
            {
                Type = "function",
                Function = new GroqFunctionDto
                {
                    Name = "search_products",
                    Description = "Search for products by name globally. Returns stock availability, best prices and distance from user.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            productName = new { type = "string", description = "The simple name of the product only (e.g. 'apple', 'bread'). DO NOT include quantities or full phrases." },
                            quantity = new { type = "integer", description = "The required quantity." }
                        },
                        required = new[] { "productName", "quantity" }
                    }
                }
            },
            new GroqToolDto
            {
                Type = "function",
                Function = new GroqFunctionDto
                {
                    Name = "add_to_cart",
                    Description = "Add a product to the authenticated user's cart. " +
                        "CRITICAL: ONLY call this tool when the user has EXPLICITLY said 'add to cart', 'buy this', 'add this item', or an equivalent phrase. " +
                        "NEVER call this after a search alone. Searching for a product is NOT a request to add it. " +
                        "You MUST confirm with the user which product to add if multiple were found. " +
                        "Only confirm success if the backend returns success==true or status=='success'.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            productId = new { type = "integer", description = "The ID of the product." },
                            quantity = new { type = "integer", description = "The quantity to add." }
                        },
                        required = new[] { "productId", "quantity" }
                    }
                }
            },
            new GroqToolDto
            {
                Type = "function",
                Function = new GroqFunctionDto
                {
                    Name = "update_quantity",
                    Description = "Update the quantity of a product already in the user's cart.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            productId = new { type = "integer", description = "The ID of the product." },
                            quantity = new { type = "integer", description = "The new quantity." }
                        },
                        required = new[] { "productId", "quantity" }
                    }
                }
            },
            new GroqToolDto
            {
                Type = "function",
                Function = new GroqFunctionDto
                {
                    Name = "remove_from_cart",
                    Description = "Remove a product from the user's cart.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            productId = new { type = "integer", description = "The ID of the product." }
                        },
                        required = new[] { "productId" }
                    }
                }
            },
            new GroqToolDto
            {
                Type = "function",
                Function = new GroqFunctionDto
                {
                    Name = "track_order",
                    Description = "Track the status of an order for the authenticated user.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            orderId = new { type = "integer", description = "The ID of the order." }
                        },
                        required = new[] { "orderId" }
                    }
                }
            },
            new GroqToolDto
            {
                Type = "function",
                Function = new GroqFunctionDto
                {
                    Name = "get_delivery_eta",
                    Description = "Get the estimated delivery time for an order.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            orderId = new { type = "integer", description = "The ID of the order." }
                        },
                        required = new[] { "orderId" }
                    }
                }
            },
            new GroqToolDto
            {
                Type = "function",
                Function = new GroqFunctionDto
                {
                    Name = "get_refund_status",
                    Description = "Check the refund status of a cancelled order.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            orderId = new { type = "integer", description = "The ID of the order." }
                        },
                        required = new[] { "orderId" }
                    }
                }
            },
            new GroqToolDto
            {
                Type = "function",
                Function = new GroqFunctionDto
                {
                    Name = "get_order_details",
                    Description = "Get the full details of an order including its items.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            orderId = new { type = "integer", description = "The ID of the order." }
                        },
                        required = new[] { "orderId" }
                    }
                }
            },
            new GroqToolDto
            {
                Type = "function",
                Function = new GroqFunctionDto
                {
                    Name = "find_best_store",
                    Description = "Find the best store for a product based on customer location, comparing distance, price, and stock availability. Returns the best overall, cheapest, and nearest options.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            productName = new { type = "string", description = "The name of the product to find." },
                            quantity = new { type = "integer", description = "Required quantity." },
                            customerLatitude = new { type = "number", description = "Customer's latitude coordinate." },
                            customerLongitude = new { type = "number", description = "Customer's longitude coordinate." }
                        },
                        required = new[] { "productName", "quantity", "customerLatitude", "customerLongitude" }
                    }
                }
            },
            new GroqToolDto
            {
                Type = "function",
                Function = new GroqFunctionDto
                {
                    Name = "get_recommendations",
                    Description = "Get personalized product recommendations for the user. Includes frequently bought together, related by category, smart upsells, and purchase-history-based suggestions. Optionally scoped to a specific product.",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            productId = new { type = "integer", description = "Optional product ID to get recommendations relative to a specific product. Pass 0 or omit for general recommendations." }
                        },
                        required = Array.Empty<string>()
                    }
                }
            },
            new GroqToolDto
            {
                Type = "function",
                Function = new GroqFunctionDto
                {
                    Name = "get_cart",
                    Description = "Get the current contents of the user's shopping cart, including products, quantities, and total price.",
                    Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
                }
            }
        };
    }

    private async Task<ChatIterationResult> SendRequestAsync(GroqChatRequestDto requestDto, CancellationToken cancellationToken)
    {
        requestDto.Stream = null;
        var jsonContent = JsonSerializer.Serialize(requestDto, _jsonOptions);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Groq API returned an error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
            
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new Exception("The AI is currently experiencing high demand (Rate Limit Reached). Please try again in a few minutes.");
            }

            response.EnsureSuccessStatusCode();
        }

        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseDto = JsonSerializer.Deserialize<GroqChatResponseDto>(responseString, _jsonOptions);
        var responseMessage = responseDto?.Choices?.FirstOrDefault()?.Message;

        return new ChatIterationResult
        {
            Content = responseMessage?.Content ?? string.Empty,
            ToolCalls = responseMessage?.ToolCalls ?? new List<GroqToolCallDto>()
        };
    }

    private async Task<ChatIterationResult> SendStreamingRequestAsync(
        GroqChatRequestDto requestDto,
        Func<string, CancellationToken, Task> onToken,
        CancellationToken cancellationToken)
    {
        requestDto.Stream = true;

        var jsonContent = JsonSerializer.Serialize(requestDto, _jsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Groq streaming API returned an error: {StatusCode} - {ErrorContent}",
                response.StatusCode,
                errorContent);
                
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new Exception("The AI is currently experiencing high demand (Rate Limit Reached). Please try again in a few minutes.");
            }

            response.EnsureSuccessStatusCode();
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var fullResponseBuilder = new StringBuilder();
        var toolCallAccumulators = new Dictionary<int, StreamingToolCallAccumulator>();

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var payload = line["data:".Length..].Trim();
            if (string.Equals(payload, "[DONE]", StringComparison.Ordinal))
            {
                break;
            }

            GroqStreamChunkDto? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<GroqStreamChunkDto>(payload, _jsonOptions);
            }
            catch (JsonException jsonException)
            {
                _logger.LogWarning(jsonException, "Unable to parse Groq stream chunk: {Payload}", payload);
                continue;
            }

            var choice = chunk?.Choices?.FirstOrDefault();
            var delta = choice?.Delta;
            if (delta is null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(delta.Content))
            {
                fullResponseBuilder.Append(delta.Content);
                await onToken(delta.Content, cancellationToken);
            }

            if (delta.ToolCalls is { Count: > 0 })
            {
                MergeToolCallDeltas(toolCallAccumulators, delta.ToolCalls);
            }
        }

        var toolCalls = BuildToolCalls(toolCallAccumulators);
        return new ChatIterationResult
        {
            Content = fullResponseBuilder.ToString(),
            ToolCalls = toolCalls
        };
    }

    private static void MergeToolCallDeltas(
        Dictionary<int, StreamingToolCallAccumulator> accumulators,
        IReadOnlyCollection<GroqStreamToolCallDeltaDto> deltas)
    {
        foreach (var delta in deltas)
        {
            if (!accumulators.TryGetValue(delta.Index, out var accumulator))
            {
                accumulator = new StreamingToolCallAccumulator(delta.Index);
                accumulators[delta.Index] = accumulator;
            }

            if (!string.IsNullOrWhiteSpace(delta.Id))
            {
                accumulator.Id = delta.Id;
            }

            if (!string.IsNullOrWhiteSpace(delta.Type))
            {
                accumulator.Type = delta.Type;
            }

            if (!string.IsNullOrWhiteSpace(delta.Function?.Name))
            {
                accumulator.Name = delta.Function.Name;
            }

            if (!string.IsNullOrEmpty(delta.Function?.Arguments))
            {
                accumulator.ArgumentsBuilder.Append(delta.Function.Arguments);
            }
        }
    }

    private static List<GroqToolCallDto> BuildToolCalls(Dictionary<int, StreamingToolCallAccumulator> accumulators)
    {
        return accumulators
            .OrderBy(kvp => kvp.Key)
            .Select(kvp =>
            {
                var value = kvp.Value;
                return new GroqToolCallDto
                {
                    Id = string.IsNullOrWhiteSpace(value.Id) ? $"call_{Guid.NewGuid():N}" : value.Id,
                    Type = string.IsNullOrWhiteSpace(value.Type) ? "function" : value.Type,
                    Function = new GroqToolCallFunctionDto
                    {
                        Name = value.Name ?? string.Empty,
                        Arguments = value.ArgumentsBuilder.ToString()
                    }
                };
            })
            .ToList();
    }

    private class SearchProductsArgs
    {
        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

    private class CartActionArgs
    {
        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

    private class OrderActionArgs
    {
        [JsonPropertyName("orderId")]
        public int OrderId { get; set; }
    }

    private class FindBestStoreArgs
    {
        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("customerLatitude")]
        public double CustomerLatitude { get; set; }

        [JsonPropertyName("customerLongitude")]
        public double CustomerLongitude { get; set; }
    }

    private class RecommendationArgs
    {
        [JsonPropertyName("productId")]
        public int ProductId { get; set; }
    }

    private sealed class ChatIterationResult
    {
        public string Content { get; init; } = string.Empty;
        public List<GroqToolCallDto> ToolCalls { get; init; } = new();
    }

    private sealed class StreamingToolCallAccumulator
    {
        public StreamingToolCallAccumulator(int index)
        {
            Index = index;
        }

        public int Index { get; }
        public string? Id { get; set; }
        public string? Type { get; set; }
        public string? Name { get; set; }
        public StringBuilder ArgumentsBuilder { get; } = new();
    }
}
