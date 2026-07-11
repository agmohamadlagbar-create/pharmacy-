using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Services.Agents;

namespace OnlineStore.Api.Services.Agents.Customer;

public class ShoppingAgent : BaseAgent
{
    private readonly ApplicationDbContext _db;
    private readonly IConversationStateManager _stateManager;
    private readonly ICartService _cartService;
    private readonly IProductSearchService _productSearchService;
    private readonly ICacheService _cache;

    public override string AgentName => "shopping";

    public override string SystemPrompt =>
        "You are a specialized shopping assistant for an e-commerce platform.\n\n" +
        "YOUR RESPONSIBILITIES:\n" +
        "- Product search and discovery\n" +
        "- Product comparison and filtering\n" +
        "- Cart management (add, update, remove items)\n" +
        "- Finding the best store for a product\n" +
        "- Helping customers choose between products\n\n" +
        "STRICT RULES:\n" +
        "1. NEVER hallucinate actions. Only confirm success if the backend explicitly says so.\n" +
        "2. The backend is the ONLY source of truth. Never invent prices, stock, or availability.\n" +
        "3. Searching for a product DOES NOT mean adding it to the cart.\n" +
        "4. Only add items when the user EXPLICITLY says: 'add to cart', 'buy this', or equivalent.\n" +
        "5. If multiple products are found, ask which one the user wants.\n" +
        "6. If search returns 0 results, say the product was not found. Do NOT suggest it was added.\n" +
        "7. If add-to-cart fails, clearly explain the failure. Do NOT pretend it succeeded.\n" +
        "8. Always fetch the latest cart state before discussing the cart.\n\n" +
        "LANGUAGE:\n" +
        "- Detect and respond in the customer's language.\n" +
        "- Backend logic must remain identical regardless of language.\n\n" +
        "NEVER discuss: order tracking, refunds, returns, complaints, delivery status.\n" +
        "If asked about those topics, say: 'Let me connect you with our support team for that.'\n\n" +
        "Always behave like a reliable production shopping assistant, not a guessing chatbot.";

    private const string IntentExtractionPrompt = 
        "You are an intent extraction assistant for an e-commerce platform.\n" +
        "Analyze the user's message and history, then output exactly ONE JSON object representing their shopping intent.\n\n" +
        "Available Intents:\n" +
        "- \"Search\": Search/browse products (e.g., 'find apples', 'show phones')\n" +
        "- \"AddToCart\": Add a product to cart (e.g., 'add to cart', 'buy two from the first option', 'add 2 of the cheapest ones', 'add two')\n" +
        "- \"UpdateQuantity\": Modify quantity of an item in cart (e.g., 'actually make it three', 'change quantity to 5')\n" +
        "- \"RemoveFromCart\": Remove an item from cart (e.g., 'remove apples from my cart', 'delete the first option')\n" +
        "- \"GetCart\": View current cart (e.g., 'show my cart', 'what is in my cart')\n" +
        "- \"GeneralChat\": General questions or talk that don't perform actions\n\n" +
        "JSON Schema:\n" +
        "{\n" +
        "  \"intent\": \"<Search|AddToCart|UpdateQuantity|RemoveFromCart|GetCart|GeneralChat>\",\n" +
        "  \"productReference\": \"<extracted product reference or null>\",\n" +
        "  \"quantity\": <extracted quantity as integer or null>\n" +
        "}\n\n" +
        "RULES:\n" +
        "1. Extract 'productReference' exactly as the user referred to it (e.g., 'first option', 'cheapest', 'apple', 'it'). Do NOT map it to a database ID.\n" +
        "2. If the user refers to an option number (e.g., 'first option', '2nd one'), output exactly 'first option', 'second option', etc.\n" +
        "3. Extract 'quantity' as an integer (e.g., 2, 3). If no quantity is specified, output null.\n" +
        "4. Output ONLY the JSON block. No explanation, no markdown styling.";

    public ShoppingAgent(
        AiGateway gateway,
        ToolRegistry toolRegistry,
        IChatHistoryService chatHistory,
        ILogger<ShoppingAgent> logger,
        ApplicationDbContext db,
        IConversationStateManager stateManager,
        ICartService cartService,
        IProductSearchService productSearchService,
        ICacheService cache)
        : base(gateway, toolRegistry, chatHistory, logger)
    {
        _db = db;
        _stateManager = stateManager;
        _cartService = cartService;
        _productSearchService = productSearchService;
        _cache = cache;
    }

    protected override bool ShouldReflect(AgentContext context) => false; // handled deterministically

    public override async Task<AgentResult> ProcessAsync(
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _db.AiConversations
            .AsTracking()
            .FirstOrDefaultAsync(c => c.Id == context.ConversationId, cancellationToken);

        if (conversation == null)
        {
            return new AgentResult { AgentName = AgentName, ResponseText = "Error: Conversation not found.", Success = false };
        }

        // 1. Intent Extraction
        ShoppingIntent extractedIntent;
        
        // Check if there is an override to execute the pending intent
        if (context.SystemPromptVariant == "[EXECUTE_PENDING_INTENT]" && !string.IsNullOrEmpty(conversation.State.PendingIntentJson))
        {
            extractedIntent = JsonSerializer.Deserialize<ShoppingIntent>(conversation.State.PendingIntentJson) 
                              ?? new ShoppingIntent { Intent = "GeneralChat" };
        }
        else
        {
            extractedIntent = await ExtractShoppingIntentAsync(context, cancellationToken);
            
            // Merge with PendingIntent if awaiting confirmation and the user message was an edit instruction
            if (conversation.State.AwaitingConfirmation && !string.IsNullOrEmpty(conversation.State.PendingIntentJson))
            {
                var pending = JsonSerializer.Deserialize<ShoppingIntent>(conversation.State.PendingIntentJson);
                if (pending != null)
                {
                    if (extractedIntent.Intent == "UpdateQuantity" || extractedIntent.Intent == "AddToCart")
                    {
                        bool modified = false;
                        if (extractedIntent.Quantity.HasValue && extractedIntent.Quantity.Value != pending.Quantity)
                        {
                            pending.Quantity = extractedIntent.Quantity.Value;
                            modified = true;
                        }
                        if (!string.IsNullOrEmpty(extractedIntent.ProductReference) && extractedIntent.ProductReference != pending.ProductReference)
                        {
                            pending.ProductReference = extractedIntent.ProductReference;
                            modified = true;
                        }
                        extractedIntent = pending;

                        if (modified)
                        {
                            conversation.State.AwaitingConfirmation = false;
                        }
                    }
                }
            }
        }

        Logger.LogInformation("[ShoppingAgent] Extracted Shopping Intent: {Intent} | Ref: {Ref} | Qty: {Qty}", 
            extractedIntent.Intent, extractedIntent.ProductReference, extractedIntent.Quantity);

        // 2. Execute Structured Pipeline
        string outcomeDescription = string.Empty;
        bool confirmationAwaited = false;

        switch (extractedIntent.Intent)
        {
            case "Search":
                outcomeDescription = await HandleSearchIntentAsync(context, extractedIntent, cancellationToken);
                break;

            case "AddToCart":
                var addRes = await HandleAddToCartIntentAsync(context, conversation, extractedIntent, cancellationToken);
                outcomeDescription = addRes.Outcome;
                confirmationAwaited = addRes.AwaitingConfirmation;
                break;

            case "UpdateQuantity":
                var updRes = await HandleUpdateQuantityIntentAsync(context, conversation, extractedIntent, cancellationToken);
                outcomeDescription = updRes.Outcome;
                confirmationAwaited = updRes.AwaitingConfirmation;
                break;

            case "RemoveFromCart":
                outcomeDescription = await HandleRemoveFromCartIntentAsync(context, conversation, extractedIntent, cancellationToken);
                break;

            case "GetCart":
                outcomeDescription = await HandleGetCartIntentAsync(context, cancellationToken);
                break;

            default:
                // GeneralChat or unrecognized
                var responseText = await HandleGeneralChatAsync(context, cancellationToken);
                return new AgentResult { AgentName = AgentName, ResponseText = responseText, Success = true };
        }

        // 3. Save Context State back to database
        if (confirmationAwaited)
        {
            conversation.State.PendingIntentJson = JsonSerializer.Serialize(extractedIntent);
            conversation.State.AwaitingConfirmation = true;
        }
        else
        {
            conversation.State.PendingIntentJson = null;
            conversation.State.AwaitingConfirmation = false;
        }
        
        conversation.State.LastShoppingIntentJson = JsonSerializer.Serialize(extractedIntent);
        conversation.State.LastSuccessfulToolResult = outcomeDescription;
        await _db.SaveChangesAsync(cancellationToken);

        // 4. Generate Natural Language Response based on verified outcome
        var friendlyResponse = await GenerateFriendlyResponseAsync(context, outcomeDescription, cancellationToken);

        return new AgentResult
        {
            AgentName = AgentName,
            ResponseText = friendlyResponse,
            Success = true,
            StructuredData = JsonSerializer.Serialize(extractedIntent),
            StructuredDataType = "shopping_intent"
        };
    }

    public override async Task<AgentResult> ProcessStreamingAsync(
        AgentContext context,
        Func<string, CancellationToken, Task> onToken,
        CancellationToken cancellationToken = default)
    {
        var result = await ProcessAsync(context, cancellationToken);
        
        if (result.Success && !string.IsNullOrEmpty(result.ResponseText))
        {
            if (result.StructuredData != null)
            {
                try
                {
                    var intent = JsonSerializer.Deserialize<ShoppingIntent>(result.StructuredData);
                    if (intent != null && (intent.Intent is "AddToCart" or "UpdateQuantity" or "RemoveFromCart"))
                    {
                        if (result.ResponseText.Contains("Successfully", StringComparison.OrdinalIgnoreCase) || 
                            result.ResponseText.Contains("updated", StringComparison.OrdinalIgnoreCase) ||
                            result.ResponseText.Contains("removed", StringComparison.OrdinalIgnoreCase) ||
                            result.ResponseText.Contains("added", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!result.ResponseText.Contains("confirm", StringComparison.OrdinalIgnoreCase))
                            {
                                await onToken("[SYSTEM_EVENT:cart_updated]", cancellationToken);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore parsing errors
                }
            }

            await onToken(result.ResponseText, cancellationToken);
        }

        return result;
    }

    // ═══════════════════════════════════════════════════
    //  Intent Handlers
    // ═══════════════════════════════════════════════════

    private async Task<string> HandleSearchIntentAsync(AgentContext context, ShoppingIntent intent, CancellationToken ct)
    {
        var searchTerm = intent.ProductReference ?? string.Empty;
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return "Search query was empty. Please provide a search term.";
        }

        var products = await DiscoverProductsAsync(context.UserId, searchTerm, "available", ct);
        if (!products.Any())
        {
            return $"No products found matching '{searchTerm}'.";
        }

        var presentedList = products.Select((p, idx) => new PresentedProductInfo
        {
            Index = idx + 1,
            ProductId = p.Id,
            Name = p.Name,
            Price = p.Price,
            Description = p.Description
        }).ToList();

        _stateManager.SavePresentedProducts(context.ConversationId, presentedList);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Product discovery returned {products.Count} results:");
        foreach (var p in presentedList)
        {
            sb.AppendLine($"Option {p.Index}: ID={p.ProductId}, Name={p.Name}, Price={p.Price:C}");
        }
        return sb.ToString();
    }

    private async Task<(string Outcome, bool AwaitingConfirmation)> HandleAddToCartIntentAsync(
        AgentContext context, 
        AiConversation conversation, 
        ShoppingIntent intent, 
        CancellationToken ct)
    {
        var qty = intent.Quantity ?? 1;

        // Resolve reference
        int? productId = await _stateManager.ResolveProductReferenceAsync(
            context.ConversationId, intent.ProductReference, context.UserMessage, _db);

        // Fallback: If not found in presented products, do direct discovery lookup
        if (!productId.HasValue && !string.IsNullOrWhiteSpace(intent.ProductReference))
        {
            var discovered = await DiscoverProductsAsync(context.UserId, intent.ProductReference, "available", ct);
            if (discovered.Any())
            {
                productId = discovered[0].Id;
            }
        }

        if (!productId.HasValue)
        {
            return ("Error: Could not resolve product reference. Please search for the product first or clarify which option you want.", false);
        }

        // Validate product and stock
        var product = await _db.Products.FindAsync(new object[] { productId.Value }, ct);
        if (product == null)
        {
            return ($"Error: Product ID {productId} does not exist.", false);
        }

        if (product.StockQuantity < qty)
        {
            return ($"Error: Only {product.StockQuantity} items in stock for '{product.Name}'. You requested {qty}.", false);
        }

        // A/B confirmation prompt check: If quantity is significant or they just resolved it, await confirmation
        if (!conversation.State.AwaitingConfirmation)
        {
            conversation.State.PendingProductId = product.Id;
            conversation.State.PendingProductName = product.Name;
            conversation.State.PendingQuantity = qty;
            conversation.State.PendingAction = "add_to_cart";
            return ($"Awaiting confirmation to add {qty}x '{product.Name}' (ID {product.Id}) to your cart.", true);
        }

        // Execute Cart Operation
        var result = await _cartService.AddToCartAsync(context.UserId, product.Id, qty);
        if (result.StartsWith("Error"))
        {
            return (result, false);
        }

        // Verify Cart Status
        var cart = await _db.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == context.UserId, ct);

        var cartItem = cart?.Items.FirstOrDefault(i => i.ProductId == product.Id);
        if (cartItem == null)
        {
            return ("Error: Verification failed. Product was not found in the cart after adding.", false);
        }

        conversation.State.SelectedProductId = product.Id;
        return ($"Successfully added {qty}x '{product.Name}' to your cart. Cart item quantity is verified as {cartItem.Quantity}.", false);
    }

    private async Task<(string Outcome, bool AwaitingConfirmation)> HandleUpdateQuantityIntentAsync(
        AgentContext context, 
        AiConversation conversation, 
        ShoppingIntent intent, 
        CancellationToken ct)
    {
        var qty = intent.Quantity ?? 1;

        int? productId = await _stateManager.ResolveProductReferenceAsync(
            context.ConversationId, intent.ProductReference, context.UserMessage, _db);

        if (!productId.HasValue)
        {
            // Default to previously selected product
            productId = conversation.State.SelectedProductId;
        }

        if (!productId.HasValue)
        {
            return ("Error: No product selection found to update quantity.", false);
        }

        var product = await _db.Products.FindAsync(new object[] { productId.Value }, ct);
        if (product == null)
        {
            return ($"Error: Product ID {productId} not found.", false);
        }

        if (product.StockQuantity < qty)
        {
            return ($"Error: Only {product.StockQuantity} items in stock for '{product.Name}'. You requested {qty}.", false);
        }

        if (!conversation.State.AwaitingConfirmation)
        {
            conversation.State.PendingProductId = product.Id;
            conversation.State.PendingProductName = product.Name;
            conversation.State.PendingQuantity = qty;
            conversation.State.PendingAction = "update_quantity";
            return ($"Awaiting confirmation to update quantity of '{product.Name}' to {qty}.", true);
        }

        var result = await _cartService.UpdateQuantityAsync(context.UserId, product.Id, qty);
        if (result.StartsWith("Error"))
        {
            return (result, false);
        }

        // Verification
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == context.UserId, ct);
        var cartItem = cart?.Items.FirstOrDefault(i => i.ProductId == product.Id);
        
        if (cartItem == null || cartItem.Quantity != qty)
        {
            return ($"Error: Verification failed. Cart quantity is not {qty}.", false);
        }

        return ($"Successfully updated quantity of '{product.Name}' to {qty}.", false);
    }

    private async Task<string> HandleRemoveFromCartIntentAsync(
        AgentContext context, 
        AiConversation conversation, 
        ShoppingIntent intent, 
        CancellationToken ct)
    {
        int? productId = await _stateManager.ResolveProductReferenceAsync(
            context.ConversationId, intent.ProductReference, context.UserMessage, _db);

        if (!productId.HasValue)
        {
            productId = conversation.State.SelectedProductId;
        }

        if (!productId.HasValue)
        {
            return "Error: Could not resolve product reference to remove.";
        }

        var product = await _db.Products.FindAsync(new object[] { productId.Value }, ct);
        if (product == null)
        {
            return "Error: Product not found.";
        }

        var result = await _cartService.RemoveFromCartAsync(context.UserId, product.Id);
        if (result.StartsWith("Error"))
        {
            return result;
        }

        // Verification
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == context.UserId, ct);
        var cartItem = cart?.Items.FirstOrDefault(i => i.ProductId == product.Id);
        
        if (cartItem != null)
        {
            return $"Error: Verification failed. Product '{product.Name}' still exists in the cart.";
        }

        return $"Successfully removed '{product.Name}' from your cart.";
    }

    private async Task<string> HandleGetCartIntentAsync(AgentContext context, CancellationToken ct)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == context.UserId, ct);

        if (cart == null || !cart.Items.Any())
        {
            return "Your cart is currently empty.";
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Current Cart Status:");
        foreach (var item in cart.Items)
        {
            sb.AppendLine($"- {item.Quantity}x '{item.Product.Name}' (Price: {item.Product.Price:C} each)");
        }
        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════
    //  Inference & Product Discovery Service
    // ═══════════════════════════════════════════════════

    private async Task<List<Product>> DiscoverProductsAsync(
        string userId, 
        string searchTerm, 
        string filters, 
        CancellationToken ct)
    {
        // 1. Resolve User Context (city/location)
        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        var city = user?.City?.ToLowerInvariant().Trim() ?? "unknown";

        var cacheKey = $"products:{city}:{searchTerm.ToLowerInvariant().Trim()}:{filters}";

        // 2. Cache Lookup
        var cachedProductIds = await _cache.GetAsync<List<int>>(cacheKey);
        if (cachedProductIds != null)
        {
            Logger.LogInformation("[ProductDiscovery] Cache HIT for key: {CacheKey}", cacheKey);
            var cachedProducts = await _db.Products
                .Include(p => p.Category)
                .Where(p => cachedProductIds.Contains(p.Id))
                .ToListAsync(ct);

            if (cachedProducts.Any())
            {
                return cachedProducts;
            }
        }

        Logger.LogInformation("[ProductDiscovery] Cache MISS for key: {CacheKey}. Fetching from database.", cacheKey);

        // 3. Database Fallback (apply business rules)
        var storesInCity = await _db.ShopProfiles
            .Where(s => s.Location.ToLower().Contains(city))
            .Select(s => s.UserId)
            .ToListAsync(ct);

        var query = _db.Products
            .Include(p => p.Category)
            .Where(p => p.Status == ProductStatus.Approved);

        if (storesInCity.Any())
        {
            query = query.Where(p => storesInCity.Contains(p.StoreUserId));
        }

        var searchLower = searchTerm.ToLower();
        query = query.Where(p => p.Name.ToLower().Contains(searchLower) || p.Category.Name.ToLower().Contains(searchLower));

        var dbProducts = await query.ToListAsync(ct);

        // Rank results: 
        // 1. Products with enough stock for requested quantity (handled by sorting by stock level descending)
        // 2. Lowest price ascending
        var rankedProducts = dbProducts
            .OrderByDescending(p => p.StockQuantity > 0)
            .ThenBy(p => p.Price)
            .ToList();

        // Save to cache
        var productIdsToCache = rankedProducts.Select(p => p.Id).ToList();
        await _cache.SetAsync(cacheKey, productIdsToCache, TimeSpan.FromMinutes(10));

        return rankedProducts;
    }

    private async Task<ShoppingIntent> ExtractShoppingIntentAsync(AgentContext context, CancellationToken ct)
    {
        var messages = new List<GroqMessageDto>
        {
            new() { Role = "system", Content = IntentExtractionPrompt }
        };

        if (context.ConversationHistory.Count > 0)
        {
            var history = context.ConversationHistory
                .Where(m => m.Role is "user" or "assistant")
                .TakeLast(6)
                .ToList();
            messages.AddRange(history);
        }

        messages.Add(new() { Role = "user", Content = context.UserMessage });

        var requestContext = new AiRequestContext { AgentName = AgentName, Complexity = TaskComplexity.Medium };
        var result = await Gateway.SendAsync(requestContext, messages, tools: null, ct);

        try
        {
            // Strip markdown code fences if present (e.g. ```json ... ```)
            var cleanedContent = result.Content;
            var fenceMatch = Regex.Match(cleanedContent, @"```(?:json)?\s*([\s\S]*?)```");
            if (fenceMatch.Success)
                cleanedContent = fenceMatch.Groups[1].Value.Trim();

            var jsonStart = cleanedContent.IndexOf('{');
            var jsonEnd = cleanedContent.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = cleanedContent[jsonStart..(jsonEnd + 1)];
                var intent = JsonSerializer.Deserialize<ShoppingIntent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (intent != null)
                {
                    return intent;
                }
            }

            Logger.LogWarning("[ShoppingAgent] Could not find valid JSON in intent extraction response: {Response}", result.Content);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to parse shopping intent from response: {Response}", result.Content);
        }

        return new ShoppingIntent { Intent = "GeneralChat" };
    }

    private async Task<string> GenerateFriendlyResponseAsync(AgentContext context, string outcome, CancellationToken ct)
    {
        var systemPrompt = 
            "You are a friendly, helpful e-commerce shopping assistant.\n" +
            "A backend action was just processed. Your job is to describe the EXACT outcome to the user.\n\n" +
            "STRICT RULES:\n" +
            "1. Output ONLY your friendly response to the customer. Do NOT explain database columns or backend details.\n" +
            "2. Respond in the SAME language the customer used (English, Turkish, Arabic, etc.).\n" +
            "3. Use the verified backend outcome as the absolute source of truth. Never invent facts.\n" +
            "4. Keep the response concise, helpful, and invite further interaction.\n" +
            "5. If the outcome says 'Awaiting confirmation', you MUST ask the user to confirm. Do NOT say the action was completed.\n" +
            "6. If the outcome starts with 'Error', explain the problem clearly. Do NOT pretend it succeeded.\n" +
            "7. Only say an item was added/updated/removed if the outcome explicitly says 'Successfully'.\n\n" +
            $"[Verified Backend Outcome]:\n{outcome}";

        var messages = new List<GroqMessageDto>
        {
            new() { Role = "system", Content = systemPrompt }
        };

        if (context.ConversationHistory.Count > 0)
        {
            var history = context.ConversationHistory
                .Where(m => m.Role is "user" or "assistant")
                .TakeLast(4)
                .ToList();
            messages.AddRange(history);
        }
        messages.Add(new() { Role = "user", Content = context.UserMessage });

        var requestContext = new AiRequestContext { AgentName = AgentName, Complexity = TaskComplexity.Medium };
        var result = await Gateway.SendAsync(requestContext, messages, tools: null, ct);
        return result.Content;
    }

    private async Task<string> HandleGeneralChatAsync(AgentContext context, CancellationToken ct)
    {
        var chatPrompt = SystemPrompt + "\n\n" +
            "CRITICAL: You are in general chat mode. You do NOT have access to any backend tools right now.\n" +
            "You CANNOT add items to the cart, search for products, or perform any shopping actions in this mode.\n" +
            "If the user is asking for a shopping action (search, add to cart, etc.), tell them to rephrase their request more clearly " +
            "so the system can process it properly.\n" +
            "NEVER claim you have performed an action (added to cart, searched, etc.) — you have not.";

        var messages = new List<GroqMessageDto>
        {
            new() { Role = "system", Content = chatPrompt }
        };

        if (context.ConversationHistory.Count > 0)
        {
            var history = context.ConversationHistory
                .Where(m => m.Role is "user" or "assistant")
                .TakeLast(6)
                .ToList();
            messages.AddRange(history);
        }
        messages.Add(new() { Role = "user", Content = context.UserMessage });

        var requestContext = new AiRequestContext { AgentName = AgentName, Complexity = TaskComplexity.Medium };
        var result = await Gateway.SendAsync(requestContext, messages, tools: null, ct);
        return result.Content;
    }
}
