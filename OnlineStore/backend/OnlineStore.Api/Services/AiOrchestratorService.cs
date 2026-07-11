using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Helpers;
using OnlineStore.Api.Services.Agents;
using OnlineStore.Api.Services.Agents.Admin;
using OnlineStore.Api.Services.Agents.Customer;
using OnlineStore.Api.Services.Agents.ShopOwner;
using OnlineStore.Infrastructure.Persistence;
using OnlineStore.Infrastructure.Auth;
using OnlineStore.Domain.Entities;
using System.Text.RegularExpressions;

namespace OnlineStore.Api.Services;

/// <summary>
/// AI Gateway — the central orchestrator for the multi-agent ecosystem.
/// Preserves all existing deterministic intercepts (checkout, cart confirmations),
/// then routes to the Planner→Specialist→Reflection pipeline.
/// </summary>
public class AiOrchestratorService : IAiOrchestratorService
{
    // Legacy service (kept for backward compatibility / fallback)
    private readonly IGroqAiService _groqAiService;
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cartService;
    private readonly IProductSearchService _productSearchService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly IFeedbackService _feedbackService;
    private readonly IConversationStateManager _conversationStateManager;
    private readonly ILogger<AiOrchestratorService> _logger;

    // Multi-agent system
    private readonly PlannerAgent _plannerAgent;
    private readonly ShoppingAgent _shoppingAgent;
    private readonly CustomerServiceAgent _customerServiceAgent;
    private readonly RecommendationAgent _recommendationAgent;
    private readonly OrderTrackingAgent _orderTrackingAgent;
    private readonly ReflectionAgent _reflectionAgent;
    private readonly GeneralChatAgent _generalChatAgent;

    // Shop owner agents
    private readonly ShopOwnerPlannerAgent _shopPlannerAgent;
    private readonly ProductManagementAgent _productManagementAgent;
    private readonly ReportingAgent _reportingAgent;
    private readonly AnalyticsAgent _analyticsAgent;
    private readonly InventoryAgent _inventoryAgent;
    private readonly MarketingAgent _marketingAgent;
    private readonly CustomerInsightAgent _customerInsightAgent;
    private readonly BusinessAdvisorAgent _businessAdvisorAgent;

    // Admin agents
    private readonly AdminPlannerAgent _adminPlannerAgent;
    private readonly FraudDetectionAgent _fraudDetectionAgent;
    private readonly SecurityMonitoringAgent _securityMonitoringAgent;
    private readonly PlatformAnalyticsAgent _platformAnalyticsAgent;
    private readonly StoreApprovalAgent _storeApprovalAgent;
    private readonly ModerationAgent _moderationAgent;
    private readonly GlobalSupportAgent _globalSupportAgent;

    private readonly IAiMemoryService _memoryService;
    private readonly IAbTestingService _abTestingService;
    private readonly IServiceScopeFactory _scopeFactory;

    public AiOrchestratorService(
        IGroqAiService groqAiService,
        ApplicationDbContext db,
        ICartService cartService,
        IProductSearchService productSearchService,
        IChatHistoryService chatHistoryService,
        IFeedbackService feedbackService,
        IConversationStateManager conversationStateManager,
        ILogger<AiOrchestratorService> logger,
        // Customer agents
        PlannerAgent plannerAgent,
        ShoppingAgent shoppingAgent,
        CustomerServiceAgent customerServiceAgent,
        RecommendationAgent recommendationAgent,
        OrderTrackingAgent orderTrackingAgent,
        ReflectionAgent reflectionAgent,
        GeneralChatAgent generalChatAgent,
        // Shop owner agents
        ShopOwnerPlannerAgent shopPlannerAgent,
        ProductManagementAgent productManagementAgent,
        ReportingAgent reportingAgent,
        AnalyticsAgent analyticsAgent,
        InventoryAgent inventoryAgent,
        MarketingAgent marketingAgent,
        CustomerInsightAgent customerInsightAgent,
        BusinessAdvisorAgent businessAdvisorAgent,
        // Admin agents
        AdminPlannerAgent adminPlannerAgent,
        FraudDetectionAgent fraudDetectionAgent,
        SecurityMonitoringAgent securityMonitoringAgent,
        PlatformAnalyticsAgent platformAnalyticsAgent,
        StoreApprovalAgent storeApprovalAgent,
        ModerationAgent moderationAgent,
        GlobalSupportAgent globalSupportAgent,
        // Advanced features
        IAiMemoryService memoryService,
        IAbTestingService abTestingService,
        IServiceScopeFactory scopeFactory)
    {
        _groqAiService = groqAiService;
        _db = db;
        _cartService = cartService;
        _productSearchService = productSearchService;
        _chatHistoryService = chatHistoryService;
        _feedbackService = feedbackService;
        _conversationStateManager = conversationStateManager;
        _logger = logger;

        _plannerAgent = plannerAgent;
        _shoppingAgent = shoppingAgent;
        _customerServiceAgent = customerServiceAgent;
        _recommendationAgent = recommendationAgent;
        _orderTrackingAgent = orderTrackingAgent;
        _reflectionAgent = reflectionAgent;
        _generalChatAgent = generalChatAgent;

        _shopPlannerAgent = shopPlannerAgent;
        _productManagementAgent = productManagementAgent;
        _reportingAgent = reportingAgent;
        _analyticsAgent = analyticsAgent;
        _inventoryAgent = inventoryAgent;
        _marketingAgent = marketingAgent;
        _customerInsightAgent = customerInsightAgent;
        _businessAdvisorAgent = businessAdvisorAgent;

        _adminPlannerAgent = adminPlannerAgent;
        _fraudDetectionAgent = fraudDetectionAgent;
        _securityMonitoringAgent = securityMonitoringAgent;
        _platformAnalyticsAgent = platformAnalyticsAgent;
        _storeApprovalAgent = storeApprovalAgent;
        _moderationAgent = moderationAgent;
        _globalSupportAgent = globalSupportAgent;

        _memoryService = memoryService;
        _abTestingService = abTestingService;
        _scopeFactory = scopeFactory;
    }

    // ═══════════════════════════════════════════════════
    //  Backward-compatible entry points (default to Customer role)
    // ═══════════════════════════════════════════════════

    public async Task<string> ProcessMessageAsync(string userMessage, string userId, int conversationId)
    {
        return await ProcessMessageInternalAsync(userMessage, userId, conversationId, "Customer", null, CancellationToken.None);
    }

    public async Task<string> ProcessMessageStreamingAsync(
        string userMessage, 
        string userId, 
        int conversationId, 
        Func<string, CancellationToken, Task> onToken, 
        CancellationToken cancellationToken = default)
    {
        return await ProcessMessageInternalAsync(userMessage, userId, conversationId, "Customer", onToken, cancellationToken);
    }

    // ═══════════════════════════════════════════════════
    //  Role-aware entry points
    // ═══════════════════════════════════════════════════

    public async Task<string> ProcessMessageAsync(string userMessage, string userId, int conversationId, string userRole)
    {
        return await ProcessMessageInternalAsync(userMessage, userId, conversationId, userRole, null, CancellationToken.None);
    }

    public async Task<string> ProcessMessageStreamingAsync(
        string userMessage, 
        string userId, 
        int conversationId, 
        string userRole,
        Func<string, CancellationToken, Task> onToken, 
        CancellationToken cancellationToken = default)
    {
        return await ProcessMessageInternalAsync(userMessage, userId, conversationId, userRole, onToken, cancellationToken);
    }

    // ═══════════════════════════════════════════════════
    //  Core processing pipeline
    // ═══════════════════════════════════════════════════

    private async Task<string> ProcessMessageInternalAsync(
        string userMessage,
        string userId,
        int conversationId,
        string userRole,
        Func<string, CancellationToken, Task>? onToken,
        CancellationToken cancellationToken)
    {
        var conversation = await _db.AiConversations
            .AsTracking()
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, cancellationToken);

        if (conversation == null)
            throw new UnauthorizedAccessException("Conversation not found.");

        // ─────────────────────────────────────────
        //  PHASE 1: Deterministic Intercepts (PRESERVED from original)
        // ─────────────────────────────────────────
        string? immediateResponse = null;

        if (conversation.State.AwaitingConfirmation)
        {
            var msgLower = userMessage.Trim().ToLowerInvariant();
            msgLower = Regex.Replace(msgLower, @"[.,!?،؟]", "").Trim();

            bool isPureConfirm = Regex.IsMatch(msgLower, @"^\s*(yes|yeah|yep|ok|okay|sure|proceed|confirm|evet|tamam|onay|نعم|تمام|موافق|أوكي)\s*$");
            
            bool isCancel = false;
            if (Regex.IsMatch(msgLower, @"^\s*(no|nope|cancel|stop|don't|hayır|iptal|istemiyorum|لا|إلغاء|اكنسل)\b"))
            {
                if (msgLower.Contains("add") || msgLower.Contains("confirm") || msgLower.Contains("proceed") || msgLower.Contains("yes") || msgLower.Contains("ok") || msgLower.Contains("keep"))
                {
                    isCancel = false;
                }
                else
                {
                    isCancel = true;
                }
            }

            if (isPureConfirm)
            {
                _logger.LogInformation("Smart Confirmation Engine intercepted pure confirmation, routing to ShoppingAgent for execution.");
                
                var conversationHistory = await _chatHistoryService.LoadRecentMessagesAsync(userId, conversationId, 20);
                var context = new AgentContext
                {
                    UserMessage = userMessage,
                    UserId = userId,
                    ConversationId = conversationId,
                    UserRole = userRole,
                    ConversationHistory = conversationHistory,
                    SystemPromptVariant = "[EXECUTE_PENDING_INTENT]"
                };

                await _chatHistoryService.SaveMessageAsync(userId, conversationId, "user", userMessage);

                var responseText = await _shoppingAgent.ProcessAsync(context, cancellationToken);
                await _chatHistoryService.SaveMessageAsync(userId, conversationId, "assistant", responseText.ResponseText);

                if (onToken != null)
                {
                    if (conversation.State.PendingAction is "add_to_cart" or "update_quantity" or "remove_from_cart")
                    {
                        await onToken("[SYSTEM_EVENT:cart_updated]", cancellationToken);
                    }
                    await onToken(responseText.ResponseText, cancellationToken);
                }

                // Clear state
                conversation.State.PendingProductId = null;
                conversation.State.PendingProductName = null;
                conversation.State.PendingQuantity = 1;
                conversation.State.PendingAction = null;
                conversation.State.PendingIntentJson = null;
                conversation.State.AwaitingConfirmation = false;
                await _db.SaveChangesAsync(cancellationToken);

                return responseText.ResponseText;
            }
            else if (isCancel)
            {
                _logger.LogInformation("Smart Confirmation Engine intercepted cancellation.");
                string cancelResponse = msgLower.Contains("لا") || msgLower.Contains("إلغاء") || msgLower.Contains("اكنسل")
                    ? "حسناً، لقد قمت بإلغاء هذا الإجراء. كيف يمكنني مساعدتك في شيء آخر؟"
                    : msgLower.Contains("hayır") || msgLower.Contains("iptal") || msgLower.Contains("istemiyorum")
                        ? "Tamam, bu işlemi iptal ettim. Başka nasıl yardımcı olabilirim?"
                        : "Okay, I have cancelled that action. What else can I help you with?";

                await _chatHistoryService.SaveMessageAsync(userId, conversationId, "user", userMessage);
                await _chatHistoryService.SaveMessageAsync(userId, conversationId, "assistant", cancelResponse);

                if (onToken != null)
                {
                    await onToken(cancelResponse, cancellationToken);
                }

                // Clear state
                conversation.State.PendingProductId = null;
                conversation.State.PendingProductName = null;
                conversation.State.PendingQuantity = 1;
                conversation.State.PendingAction = null;
                conversation.State.PendingIntentJson = null;
                conversation.State.AwaitingConfirmation = false;
                await _db.SaveChangesAsync(cancellationToken);

                return cancelResponse;
            }
            else
            {
                // Modification instruction (e.g. "ok make it two"), let it go to Multi-Agent Pipeline where ShoppingAgent will handle merging
                _logger.LogInformation("Smart Confirmation Engine detected instruction modification, routing to Multi-Agent Pipeline.");
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

        // ─────────────────────────────────────────
        //  PHASE 2: Multi-Agent Pipeline
        // ─────────────────────────────────────────
        try
        {
            // Save user message
            await _chatHistoryService.SaveMessageAsync(userId, conversationId, "user", userMessage);

            // Load conversation history
            var conversationHistory = await _chatHistoryService.LoadRecentMessagesAsync(userId, conversationId, 20);

            // Build context
            var context = new AgentContext
            {
                UserMessage = userMessage,
                UserId = userId,
                ConversationId = conversationId,
                UserRole = userRole,
                ConversationHistory = conversationHistory
            };

            // Inject Memory Context
            var memoryContext = await _memoryService.BuildMemoryContextAsync(userId, userMessage, userRole, cancellationToken);
            if (!string.IsNullOrEmpty(memoryContext))
            {
                context.UserMessage = $"{memoryContext}\nUser message: {context.UserMessage}";
            }

            string aiResponse;

            if (userRole == "Admin")
            {
                aiResponse = await ProcessAdminPipelineAsync(context, userMessage, onToken, cancellationToken);
            }
            else if (userRole == "Shop")
            {
                aiResponse = await ProcessShopOwnerPipelineAsync(context, userMessage, onToken, cancellationToken);
            }
            else
            {
                aiResponse = await ProcessCustomerPipelineAsync(context, userMessage, onToken, cancellationToken);
            }

            // Sanitize and save
            var sanitizedResponse = OutputSanitizer.Sanitize(aiResponse);
            await _chatHistoryService.SaveMessageAsync(userId, conversationId, "assistant", sanitizedResponse);

            // Update conversation metadata
            conversation.LastAgentUsed = context.ClassifiedIntent ?? "general_chat";
            conversation.AgentType = userRole == "Shop" ? "shop_owner" : "customer";
            conversation.UpdatedAtUtc = DateTime.UtcNow;
            conversation.LastMessageAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            return sanitizedResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Multi-agent pipeline failed for user {UserId}. Falling back to unified Groq/Fallback pipeline.", userId);

            // Re-load context or ensure it is clean
            var conversationHistory = await _chatHistoryService.LoadRecentMessagesAsync(userId, conversationId, 20);
            var fallbackContext = new AgentContext
            {
                UserMessage = userMessage,
                UserId = userId,
                ConversationId = conversationId,
                UserRole = userRole,
                ConversationHistory = conversationHistory
            };

            // Inject Memory Context
            var memoryContext = await _memoryService.BuildMemoryContextAsync(userId, userMessage, userRole, cancellationToken);
            if (!string.IsNullOrEmpty(memoryContext))
            {
                fallbackContext.UserMessage = $"{memoryContext}\nUser message: {fallbackContext.UserMessage}";
            }

            // Keyword-based fallback classification
            var lowerMsg = userMessage.ToLowerInvariant();
            bool isShopping = lowerMsg.Contains("shop") || lowerMsg.Contains("cart") || lowerMsg.Contains("buy") || 
                              lowerMsg.Contains("search") || lowerMsg.Contains("find") || lowerMsg.Contains("apple") || 
                              lowerMsg.Contains("option") || lowerMsg.Contains("add") || lowerMsg.Contains("quantity") || 
                              lowerMsg.Contains("remove") || lowerMsg.Contains("clear") || lowerMsg.Contains("delete");

            if (isShopping && userRole == "Customer")
            {
                _logger.LogInformation("[Orchestrator Fallback] Classified intent as SHOPPING. Routing to ShoppingAgent.");
                var result = await _shoppingAgent.ProcessAsync(fallbackContext, cancellationToken);

                if (onToken != null)
                {
                    await onToken(result.ResponseText, cancellationToken);
                }
                return result.ResponseText;
            }

            // Non-shopping fallback: use Groq fallback directly
            string aiResponse;
            if (onToken == null)
            {
                aiResponse = await _groqAiService.AskAsync(userMessage, userId, conversationId);
            }
            else
            {
                aiResponse = await _groqAiService.AskStreamingAsync(userMessage, userId, conversationId, onToken, cancellationToken);
            }

            return OutputSanitizer.Sanitize(aiResponse);
        }
    }

    // ═══════════════════════════════════════════════════
    //  Customer Pipeline: Planner → Specialist → Reflection
    // ═══════════════════════════════════════════════════

    private async Task<string> ProcessCustomerPipelineAsync(
        AgentContext context,
        string originalUserMessage,
        Func<string, CancellationToken, Task>? onToken,
        CancellationToken cancellationToken)
    {
        // Step 1: Plan — classify intent
        var planResult = await _plannerAgent.ProcessAsync(context, cancellationToken);
        var plan = PlannerAgent.ParsePlan(planResult.ResponseText);
        context.ClassifiedIntent = plan.Intent;
        context.PlannedTasks = plan.Tasks;

        _logger.LogInformation("[Gateway] Customer pipeline: intent={Intent}, tasks=[{Tasks}]",
            plan.Intent, string.Join(", ", plan.Tasks));


        // Step 2: Route to specialist agent
        var specialist = ResolveCustomerAgent(plan.Intent);

        // A/B test variant assignment
        var variant = await _abTestingService.GetVariantForConversationAsync(specialist.AgentName, context.ConversationId);
        if (variant != null)
        {
            context.SystemPromptVariant = variant.SystemPrompt;
        }

        AgentResult specialistResult;

        if (onToken != null)
        {
            // Streaming: specialist emits tokens directly
            specialistResult = await specialist.ProcessStreamingAsync(context, onToken, cancellationToken);
        }
        else
        {
            specialistResult = await specialist.ProcessAsync(context, cancellationToken);
        }

        // Detect implicit negative signals after specialist response
        await _feedbackService.DetectAndRecordImplicitFeedbackAsync(
            context.UserId, context.ConversationId, originalUserMessage, specialist.AgentName);

        // Run fire-and-forget memory updates (preferences, facts, episodes) using background scope
        var scopeId = context.ConversationId;
        var scopeUser = context.UserId;
        var scopeResponse = specialistResult.ResponseText;
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var memoryService = scope.ServiceProvider.GetRequiredService<IAiMemoryService>();
                await memoryService.InferPreferencesFromConversationAsync(scopeUser, scopeId);
                await memoryService.ExtractAndSaveMemoriesAsync(scopeUser, scopeId, originalUserMessage, scopeResponse);
                await memoryService.DetectAndRecordEpisodesAsync(scopeUser, scopeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing memory updates in background thread for conversation {ConversationId}", scopeId);
            }
        });

        // Step 3: Reflection (only for important responses, NOT for streaming since tokens are already sent)
        if (specialistResult.RequiresReflection && onToken == null)
        {
            var reflectedResult = await _reflectionAgent.ReflectAsync(
                context.UserMessage,
                specialistResult.ResponseText,
                specialist.AgentName,
                cancellationToken);

            return reflectedResult.ResponseText;
        }

        return specialistResult.ResponseText;
    }

    // ═══════════════════════════════════════════════════
    //  Shop Owner Pipeline: Planner → Specialist
    // ═══════════════════════════════════════════════════

    private async Task<string> ProcessShopOwnerPipelineAsync(
        AgentContext context,
        string originalUserMessage,
        Func<string, CancellationToken, Task>? onToken,
        CancellationToken cancellationToken)
    {
        // Step 1: Plan
        var planResult = await _shopPlannerAgent.ProcessAsync(context, cancellationToken);
        var plan = PlannerAgent.ParsePlan(planResult.ResponseText);
        context.ClassifiedIntent = plan.Intent;

        _logger.LogInformation("[Gateway] Shop owner pipeline: intent={Intent}", plan.Intent);

        // Step 2: Route to specialist
        var specialist = ResolveShopOwnerAgent(plan.Intent);

        AgentResult specialistResult;
        if (onToken != null)
        {
            specialistResult = await specialist.ProcessStreamingAsync(context, onToken, cancellationToken);
        }
        else
        {
            specialistResult = await specialist.ProcessAsync(context, cancellationToken);
        }

        // Detect implicit negative signals after specialist response
        await _feedbackService.DetectAndRecordImplicitFeedbackAsync(
            context.UserId, context.ConversationId, originalUserMessage, specialist.AgentName);

        return specialistResult.ResponseText;
    }

    // ═══════════════════════════════════════════════════
    //  Agent Resolution
    // ═══════════════════════════════════════════════════

    private IAgent ResolveCustomerAgent(string intent) => intent switch
    {
        "shopping" => _shoppingAgent,
        "support" => _customerServiceAgent,
        "recommendation" => _recommendationAgent,
        "order_tracking" => _orderTrackingAgent,
        "general_chat" => _generalChatAgent,
        _ => _generalChatAgent
    };

    private IAgent ResolveShopOwnerAgent(string intent) => intent switch
    {
        "product_management" => _productManagementAgent,
        "reporting" => _reportingAgent,
        "analytics" => _analyticsAgent,
        "inventory" => _inventoryAgent,
        "marketing" => _marketingAgent,
        "customer_insights" => _customerInsightAgent,
        "business_advisor" => _businessAdvisorAgent,
        "general_chat" => _generalChatAgent,
        _ => _generalChatAgent
    };

    // ═══════════════════════════════════════════════════
    //  Admin Pipeline: Planner → Specialist
    // ═══════════════════════════════════════════════════

    private async Task<string> ProcessAdminPipelineAsync(
        AgentContext context,
        string originalUserMessage,
        Func<string, CancellationToken, Task>? onToken,
        CancellationToken cancellationToken)
    {
        var planResult = await _adminPlannerAgent.ProcessAsync(context, cancellationToken);
        var plan = PlannerAgent.ParsePlan(planResult.ResponseText);
        context.ClassifiedIntent = plan.Intent;

        _logger.LogInformation("[Gateway] Admin pipeline: intent={Intent}", plan.Intent);

        var specialist = ResolveAdminAgent(plan.Intent);

        AgentResult specialistResult;
        if (onToken != null)
        {
            specialistResult = await specialist.ProcessStreamingAsync(context, onToken, cancellationToken);
        }
        else
        {
            specialistResult = await specialist.ProcessAsync(context, cancellationToken);
        }

        await _feedbackService.DetectAndRecordImplicitFeedbackAsync(
            context.UserId, context.ConversationId, originalUserMessage, specialist.AgentName);

        return specialistResult.ResponseText;
    }

    private IAgent ResolveAdminAgent(string intent) => intent switch
    {
        "fraud_detection" => _fraudDetectionAgent,
        "security_monitoring" => _securityMonitoringAgent,
        "platform_analytics" => _platformAnalyticsAgent,
        "store_approval" => _storeApprovalAgent,
        "moderation" => _moderationAgent,
        "global_support" => _globalSupportAgent,
        "general_chat" => _generalChatAgent,
        _ => _generalChatAgent
    };
}

