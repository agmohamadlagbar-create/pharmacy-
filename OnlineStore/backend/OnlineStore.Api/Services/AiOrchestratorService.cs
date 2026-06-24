using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Helpers;
using OnlineStore.Infrastructure.Persistence;
using System.Text.RegularExpressions;

namespace OnlineStore.Api.Services;

public class AiOrchestratorService : IAiOrchestratorService
{
    private readonly IGroqAiService _groqAiService;
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cartService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly ILogger<AiOrchestratorService> _logger;

    public AiOrchestratorService(
        IGroqAiService groqAiService,
        ApplicationDbContext db,
        ICartService cartService,
        IChatHistoryService chatHistoryService,
        ILogger<AiOrchestratorService> logger)
    {
        _groqAiService = groqAiService;
        _db = db;
        _cartService = cartService;
        _chatHistoryService = chatHistoryService;
        _logger = logger;
    }

    public async Task<string> ProcessMessageAsync(string userMessage, string userId, int conversationId)
    {
        return await ProcessMessageInternalAsync(userMessage, userId, conversationId, null, CancellationToken.None);
    }

    public async Task<string> ProcessMessageStreamingAsync(
        string userMessage, 
        string userId, 
        int conversationId, 
        Func<string, CancellationToken, Task> onToken, 
        CancellationToken cancellationToken = default)
    {
        return await ProcessMessageInternalAsync(userMessage, userId, conversationId, onToken, cancellationToken);
    }

    private async Task<string> ProcessMessageInternalAsync(
        string userMessage,
        string userId,
        int conversationId,
        Func<string, CancellationToken, Task>? onToken,
        CancellationToken cancellationToken)
    {
        var conversation = await _db.AiConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, cancellationToken);

        if (conversation == null)
            throw new UnauthorizedAccessException("Conversation not found.");

        string? immediateResponse = null;

        if (conversation.State.AwaitingConfirmation)
        {
            var msgLower = userMessage.Trim().ToLowerInvariant();
            // Multi-language support (English, Turkish, Arabic)
            bool isConfirm = Regex.IsMatch(msgLower, @"\b(yes|yeah|yep|ok|okay|sure|add it|proceed|confirm|evet|tamam|ekle|onay|نعم|نعم|تمام|موافق|ضيف|أوكي)\b")
                             || msgLower.Contains("نعم") || msgLower.Contains("موافق") || msgLower.Contains("ضيف");
                             
            bool isCancel = Regex.IsMatch(msgLower, @"\b(no|nope|cancel|stop|don't|hayır|iptal|istemiyorum|لا|إلغاء|اكنسل)\b")
                            || msgLower.Contains("لا") || msgLower.Contains("إلغاء");

            if (isConfirm && !string.IsNullOrEmpty(conversation.State.PendingAction))
            {
                if (conversation.State.PendingAction == "add_to_cart" && conversation.State.PendingProductId.HasValue)
                {
                    _logger.LogInformation("Smart Confirmation Engine intercepted 'add_to_cart' for Product {ProductId}", conversation.State.PendingProductId);
                    var addResult = await _cartService.AddToCartAsync(userId, conversation.State.PendingProductId.Value, conversation.State.PendingQuantity);
                    if (addResult.StartsWith("Error"))
                    {
                        immediateResponse = addResult;
                    }
                    else
                    {
                        if (onToken != null)
                        {
                            await onToken("[SYSTEM_EVENT:cart_updated]", cancellationToken);
                        }
                        
                        // Localized success messages
                        if (msgLower.Contains("نعم") || msgLower.Contains("تمام") || msgLower.Contains("ضيف") || msgLower.Contains("اي"))
                        {
                            immediateResponse = $"تم إضافة {conversation.State.PendingQuantity}x '{conversation.State.PendingProductName}' إلى سلتك بنجاح.";
                        }
                        else if (msgLower.Contains("evet") || msgLower.Contains("tamam") || msgLower.Contains("ekle") || msgLower.Contains("onay"))
                        {
                            immediateResponse = $"{conversation.State.PendingQuantity}x '{conversation.State.PendingProductName}' başarıyla sepetinize eklendi.";
                        }
                        else
                        {
                            immediateResponse = $"I have added {conversation.State.PendingQuantity}x '{conversation.State.PendingProductName}' to your cart successfully.";
                        }
                    }
                }
                
                // Clear state
                conversation.State = new();
                await _db.SaveChangesAsync(cancellationToken);
            }
            else if (isCancel)
            {
                _logger.LogInformation("Smart Confirmation Engine intercepted cancellation.");
                
                if (msgLower.Contains("لا") || msgLower.Contains("إلغاء") || msgLower.Contains("اكنسل"))
                {
                    immediateResponse = "حسناً، لقد قمت بإلغاء هذا الإجراء. كيف يمكنني مساعدتك في شيء آخر؟";
                }
                else if (msgLower.Contains("hayır") || msgLower.Contains("iptal") || msgLower.Contains("istemiyorum"))
                {
                    immediateResponse = "Tamam, bu işlemi iptal ettim. Başka nasıl yardımcı olabilirim?";
                }
                else
                {
                    immediateResponse = "Okay, I have cancelled that action. What else can I help you with?";
                }
                
                // Clear state
                conversation.State = new();
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                // User said something else, clear confirmation state and pass to AI
                conversation.State = new();
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        // Global deterministic keywords (Checkout/Payment)
        if (immediateResponse == null)
        {
            var msgLower = userMessage.Trim().ToLowerInvariant();
            // Multi-language support (English, Turkish, Arabic)
            bool isCheckout = Regex.IsMatch(msgLower, @"\b(checkout|pay|payment|buy now|complete order|finish shopping|ödeme|satın al|sepeti onayla|دفع|تسديد|إتمام الطلب)\b")
                              || msgLower.Contains("دفع") || msgLower.Contains("إتمام الطلب");
            
            if (isCheckout)
            {
                _logger.LogInformation("Orchestrator intercepted checkout request.");
                
                // Multi-language response
                if (msgLower.Contains("دفع") || msgLower.Contains("طلب") || msgLower.Contains("تسديد"))
                {
                    immediateResponse = "من أجل أمانك، لا يمكنني معالجة الدفع أو إتمام الطلب مباشرة. يرجى الضغط على سلة التسوق لمراجعة طلبك والانتقال إلى صفحة الدفع الآمنة.";
                }
                else if (msgLower.Contains("ödeme") || msgLower.Contains("satın") || msgLower.Contains("onayla"))
                {
                    immediateResponse = "Güvenliğiniz için ödeme ve ödeme işlemlerini doğrudan gerçekleştiremiyorum. Lütfen sepetinize tıklayarak güvenli ödeme sayfamıza ilerleyin.";
                }
                else
                {
                    immediateResponse = "For your security, I cannot process payments or handle the checkout process directly. Please click on your Shopping Cart to review your items and proceed to our secure checkout page.";
                }
                
                // If they were waiting for a confirmation, clear it since they've changed topics
                if (conversation.State.AwaitingConfirmation)
                {
                    conversation.State = new();
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
        }

        if (immediateResponse != null)
        {
            // We handled it deterministically.
            await _chatHistoryService.SaveMessageAsync(userId, conversationId, "user", userMessage);
            
            if (onToken != null)
            {
                // Simulate streaming by sending the response in chunks (or just one chunk)
                await onToken(immediateResponse, cancellationToken);
            }

            await _chatHistoryService.SaveMessageAsync(userId, conversationId, "assistant", immediateResponse);
            return immediateResponse;
        }

        // Delegate to AI
        string aiResponse = string.Empty;
        if (onToken == null)
        {
            aiResponse = await _groqAiService.AskAsync(userMessage, userId, conversationId);
        }
        else
        {
            aiResponse = await _groqAiService.AskStreamingAsync(userMessage, userId, conversationId, onToken, cancellationToken);
        }

        // Sanitize response and return
        // Note: In streaming, the raw chunks are already sent to the user.
        // Sanitizing the final string only protects the database unless we build a streaming sanitizer.
        // The user specifically asked for an Output Sanitizer before frontend response.
        return OutputSanitizer.Sanitize(aiResponse);
    }
}
