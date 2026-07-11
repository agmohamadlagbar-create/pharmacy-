using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class PresentedProductInfo
{
    public int Index { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Description { get; set; }
}

public class ConversationStateContext
{
    public int ConversationId { get; set; }
    public List<PresentedProductInfo> PresentedProducts { get; set; } = new();
    public PresentedProductInfo? CurrentSelection { get; set; }
    public string? CartOperationInProgress { get; set; }
    public int? PendingQuantity { get; set; }
    public DateTime LastAccessedAtUtc { get; set; } = DateTime.UtcNow;
}

public interface IConversationStateManager
{
    void SavePresentedProducts(int conversationId, List<PresentedProductInfo> products);
    List<PresentedProductInfo> GetPresentedProducts(int conversationId);
    void SetCurrentSelection(int conversationId, int productId);
    PresentedProductInfo? GetCurrentSelection(int conversationId);
    Task<int?> ResolveReferenceAsync(int conversationId, string userMessage, string userId, ApplicationDbContext db, CancellationToken ct = default);
    Task<int?> ResolveProductReferenceAsync(int conversationId, string? productReference, string userMessage, ApplicationDbContext db);
    void ClearState(int conversationId);
    void PruneStaleContexts(TimeSpan ageLimit);
}

public class ConversationStateManager : IConversationStateManager
{
    private static readonly ConcurrentDictionary<int, ConversationStateContext> _contexts = new();
    private readonly ILogger<ConversationStateManager> _logger;

    public ConversationStateManager(ILogger<ConversationStateManager> logger)
    {
        _logger = logger;
    }

    public void SavePresentedProducts(int conversationId, List<PresentedProductInfo> products)
    {
        var ctx = _contexts.GetOrAdd(conversationId, id => new ConversationStateContext { ConversationId = id });
        ctx.PresentedProducts = products;
        ctx.LastAccessedAtUtc = DateTime.UtcNow;
        _logger.LogInformation("[StateManager] Saved {Count} presented products for conversation {ConversationId}", products.Count, conversationId);
    }

    public List<PresentedProductInfo> GetPresentedProducts(int conversationId)
    {
        if (_contexts.TryGetValue(conversationId, out var ctx))
        {
            ctx.LastAccessedAtUtc = DateTime.UtcNow;
            return ctx.PresentedProducts;
        }
        return new List<PresentedProductInfo>();
    }

    public void SetCurrentSelection(int conversationId, int productId)
    {
        if (_contexts.TryGetValue(conversationId, out var ctx))
        {
            ctx.LastAccessedAtUtc = DateTime.UtcNow;
            var prod = ctx.PresentedProducts.FirstOrDefault(p => p.ProductId == productId);
            if (prod != null)
            {
                ctx.CurrentSelection = prod;
            }
        }
    }

    public PresentedProductInfo? GetCurrentSelection(int conversationId)
    {
        if (_contexts.TryGetValue(conversationId, out var ctx))
        {
            ctx.LastAccessedAtUtc = DateTime.UtcNow;
            return ctx.CurrentSelection;
        }
        return null;
    }

    public void ClearState(int conversationId)
    {
        _contexts.TryRemove(conversationId, out _);
    }

    public void PruneStaleContexts(TimeSpan ageLimit)
    {
        var cutoff = DateTime.UtcNow - ageLimit;
        var staleKeys = _contexts
            .Where(kvp => kvp.Value.LastAccessedAtUtc < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in staleKeys)
        {
            _contexts.TryRemove(key, out _);
        }

        if (staleKeys.Any())
        {
            _logger.LogInformation("[StateManager] Pruned {Count} stale conversation state contexts older than {AgeLimit}", staleKeys.Count, ageLimit);
        }
    }

    public async Task<int?> ResolveReferenceAsync(
        int conversationId, 
        string userMessage, 
        string userId,
        ApplicationDbContext db,
        CancellationToken ct = default)
    {
        if (!_contexts.TryGetValue(conversationId, out var ctx) || ctx.PresentedProducts.Count == 0)
        {
            return null;
        }

        var normalized = userMessage.ToLowerInvariant().Trim();

        // 1. Check Ordinal References
        int? resolvedIndex = null;
        if (Regex.IsMatch(normalized, @"\b(first|1st|number\s*one|number\s*1|top\s*one|cheaper\s*one|cheapest)\b"))
        {
            resolvedIndex = 0; // 0-indexed first product
        }
        else if (Regex.IsMatch(normalized, @"\b(second|2nd|number\s*two|number\s*2)\b"))
        {
            resolvedIndex = 1;
        }
        else if (Regex.IsMatch(normalized, @"\b(third|3rd|number\s*three|number\s*3)\b"))
        {
            resolvedIndex = 2;
        }
        else if (Regex.IsMatch(normalized, @"\b(last|final|final\s*one|end\s*one)\b"))
        {
            resolvedIndex = ctx.PresentedProducts.Count - 1;
        }

        PresentedProductInfo? matchedProduct = null;

        if (resolvedIndex.HasValue && resolvedIndex.Value < ctx.PresentedProducts.Count)
        {
            matchedProduct = ctx.PresentedProducts[resolvedIndex.Value];
        }

        // 2. Check Price-based References (if not resolved by ordinal cheapest/expensive)
        if (matchedProduct == null)
        {
            if (Regex.IsMatch(normalized, @"\b(cheaper|cheapest|cheap|less\s*expensive|lowest\s*price|lower\s*price)\b"))
            {
                matchedProduct = ctx.PresentedProducts.OrderBy(p => p.Price).FirstOrDefault();
            }
            else if (Regex.IsMatch(normalized, @"\b(expensive|most\s*expensive|costliest|highest\s*price|priciest)\b"))
            {
                matchedProduct = ctx.PresentedProducts.OrderByDescending(p => p.Price).FirstOrDefault();
            }
        }

        // 3. Check Context Selection ("that one", "this", "it", "those")
        if (matchedProduct == null && ctx.CurrentSelection != null)
        {
            if (Regex.IsMatch(normalized, @"\b(that\s*one|this|it|those|that|the\s*same|same)\b"))
            {
                matchedProduct = ctx.CurrentSelection;
            }
        }

        // 4. Check Brand, Color, or Name Keywords Attribute matching
        if (matchedProduct == null)
        {
            foreach (var p in ctx.PresentedProducts)
            {
                // Check if specific name keywords match
                var nameWords = p.Name.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in nameWords)
                {
                    if (word.Length > 3 && normalized.Contains(word))
                    {
                        matchedProduct = p;
                        break;
                    }
                }
                if (matchedProduct != null) break;

                if (!string.IsNullOrWhiteSpace(p.Description))
                {
                    var descWords = p.Description.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in descWords)
                    {
                        if (word.Length > 3 && normalized.Contains(word))
                        {
                            matchedProduct = p;
                            break;
                        }
                    }
                }
                if (matchedProduct != null) break;
            }
        }

        if (matchedProduct != null)
        {
            ctx.CurrentSelection = matchedProduct;

            // Update conversation state in the DB
            var conversation = await db.AiConversations.FindAsync(new object[] { conversationId }, ct);
            if (conversation != null)
            {
                conversation.State.PendingProductId = matchedProduct.ProductId;
                conversation.State.PendingProductName = matchedProduct.Name;
                conversation.State.PendingAction = "add_to_cart";
                
                // Try to parse quantity from user message (e.g. "two please", "make it three", "actually four", "2 of those")
                int qty = 1;
                var qtyMatch = Regex.Match(normalized, @"\b(one|two|three|four|five|six|seven|eight|nine|ten|\d+)\b");
                if (qtyMatch.Success)
                {
                    var val = qtyMatch.Value;
                    qty = val switch
                    {
                        "one" => 1,
                        "two" => 2,
                        "three" => 3,
                        "four" => 4,
                        "five" => 5,
                        "six" => 6,
                        "seven" => 7,
                        "eight" => 8,
                        "nine" => 9,
                        "ten" => 10,
                        _ => int.TryParse(val, out var parsed) ? parsed : 1
                    };
                }
                
                conversation.State.PendingQuantity = qty;
                conversation.State.AwaitingConfirmation = true;
                await db.SaveChangesAsync(ct);
            }

            _logger.LogInformation("[ReferenceResolver] Resolved '{UserMessage}' to Product ID {ProductId} ({Name}) for conversation {ConversationId}", 
                userMessage, matchedProduct.ProductId, matchedProduct.Name, conversationId);

            return matchedProduct.ProductId;
        }

        return null;
    }

    public async Task<int?> ResolveProductReferenceAsync(
        int conversationId, 
        string? productReference, 
        string userMessage, 
        ApplicationDbContext db)
    {
        if (!_contexts.TryGetValue(conversationId, out var ctx) || ctx.PresentedProducts.Count == 0)
        {
            return null;
        }

        var normalizedRef = (!string.IsNullOrWhiteSpace(productReference)) ? productReference.ToLowerInvariant().Trim() : string.Empty;
        var normalizedMsg = userMessage.ToLowerInvariant().Trim();

        // 1. Check Ordinal References
        int? resolvedIndex = null;
        if (System.Text.RegularExpressions.Regex.IsMatch(normalizedRef, @"\b(first|1st|number\s*one|number\s*1|top\s*one|cheaper\s*one|cheapest)\b") ||
            System.Text.RegularExpressions.Regex.IsMatch(normalizedMsg, @"\b(first|1st|number\s*one|number\s*1|top\s*one|cheaper\s*one|cheapest)\b"))
        {
            resolvedIndex = 0;
        }
        else if (System.Text.RegularExpressions.Regex.IsMatch(normalizedRef, @"\b(second|2nd|number\s*two|number\s*2)\b") ||
                 System.Text.RegularExpressions.Regex.IsMatch(normalizedMsg, @"\b(second|2nd|number\s*two|number\s*2)\b"))
        {
            resolvedIndex = 1;
        }
        else if (System.Text.RegularExpressions.Regex.IsMatch(normalizedRef, @"\b(third|3rd|number\s*three|number\s*3)\b") ||
                 System.Text.RegularExpressions.Regex.IsMatch(normalizedMsg, @"\b(third|3rd|number\s*three|number\s*3)\b"))
        {
            resolvedIndex = 2;
        }
        else if (System.Text.RegularExpressions.Regex.IsMatch(normalizedRef, @"\b(last|final|final\s*one|end\s*one)\b") ||
                 System.Text.RegularExpressions.Regex.IsMatch(normalizedMsg, @"\b(last|final|final\s*one|end\s*one)\b"))
        {
            resolvedIndex = ctx.PresentedProducts.Count - 1;
        }

        PresentedProductInfo? matchedProduct = null;

        if (resolvedIndex.HasValue && resolvedIndex.Value < ctx.PresentedProducts.Count)
        {
            matchedProduct = ctx.PresentedProducts[resolvedIndex.Value];
        }

        // 2. Check Price-based References
        if (matchedProduct == null)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(normalizedRef, @"\b(cheaper|cheapest|cheap|less\s*expensive|lowest\s*price|lower\s*price)\b") ||
                System.Text.RegularExpressions.Regex.IsMatch(normalizedMsg, @"\b(cheaper|cheapest|cheap|less\s*expensive|lowest\s*price|lower\s*price)\b"))
            {
                matchedProduct = ctx.PresentedProducts.OrderBy(p => p.Price).FirstOrDefault();
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(normalizedRef, @"\b(expensive|most\s*expensive|costliest|highest\s*price|priciest)\b") ||
                     System.Text.RegularExpressions.Regex.IsMatch(normalizedMsg, @"\b(expensive|most\s*expensive|costliest|highest\s*price|priciest)\b"))
            {
                matchedProduct = ctx.PresentedProducts.OrderByDescending(p => p.Price).FirstOrDefault();
            }
        }

        // 2b. Check Exact Price matching
        if (matchedProduct == null)
        {
            var searchSource = !string.IsNullOrWhiteSpace(normalizedRef) ? normalizedRef : normalizedMsg;
            var priceMatches = System.Text.RegularExpressions.Regex.Matches(searchSource, @"\$?(\d+(?:\.\d+)?)\b");
            foreach (System.Text.RegularExpressions.Match match in priceMatches)
            {
                if (decimal.TryParse(match.Groups[1].Value, out var parsedPrice))
                {
                    var matchingPrice = ctx.PresentedProducts.FirstOrDefault(p => Math.Abs(p.Price - parsedPrice) < 0.05m);
                    if (matchingPrice != null)
                    {
                        matchedProduct = matchingPrice;
                        break;
                    }
                }
            }
        }

        // 3. Check Context Selection ("that one", "this", "it", "those", "same")
        if (matchedProduct == null && ctx.CurrentSelection != null)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(normalizedRef, @"\b(that\s*one|this|it|those|that|the\s*same|same|last|previous)\b") ||
                System.Text.RegularExpressions.Regex.IsMatch(normalizedMsg, @"\b(that\s*one|this|it|those|that|the\s*same|same|last|previous)\b"))
            {
                matchedProduct = ctx.CurrentSelection;
            }
        }

        // Fallback: If only one product was presented, that is automatically "it"
        if (matchedProduct == null && ctx.PresentedProducts.Count == 1)
        {
            matchedProduct = ctx.PresentedProducts[0];
        }

        // 4. Check Brand, Color, or Name Keywords Attribute matching
        if (matchedProduct == null)
        {
            var searchTerms = !string.IsNullOrWhiteSpace(normalizedRef) ? normalizedRef : normalizedMsg;
            foreach (var p in ctx.PresentedProducts)
            {
                var nameWords = p.Name.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in nameWords)
                {
                    if (word.Length > 2 && searchTerms.Contains(word))
                    {
                        matchedProduct = p;
                        break;
                    }
                }
                if (matchedProduct != null) break;

                if (!string.IsNullOrWhiteSpace(p.Description))
                {
                    var descWords = p.Description.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in descWords)
                    {
                        if (word.Length > 3 && searchTerms.Contains(word))
                        {
                            matchedProduct = p;
                            break;
                        }
                    }
                }
                if (matchedProduct != null) break;
            }
        }

        if (matchedProduct != null)
        {
            ctx.CurrentSelection = matchedProduct;
            _logger.LogInformation("[ReferenceResolver] Resolved reference '{ProductReference}' (msg: '{UserMessage}') to Product ID {ProductId} ({Name})", 
                productReference, userMessage, matchedProduct.ProductId, matchedProduct.Name);
            return matchedProduct.ProductId;
        }

        return null;
    }
}
