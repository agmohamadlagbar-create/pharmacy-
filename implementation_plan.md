# Multi-Provider AI Strategy — Implementation Plan

Replace the single-provider Groq architecture with a **dual-provider AI Gateway** that routes requests to the optimal model based on task complexity, automatically falls back on rate limits, and load-balances across providers.

## Current State

All agents use a single hardcoded path:

```
Agent → GroqApiClient → Groq API (llama-3.3-70b-versatile)
```

Every request, regardless of complexity, goes to the same model. There is no fallback, no load balancing, and no cost optimization.

## Target Architecture

```
Agent
  │
  │  declares TaskComplexity
  ▼
┌─────────────────────────────────────────────┐
│              AI Gateway                      │
│                                              │
│  ┌────────────┐  ┌───────────┐  ┌────────┐ │
│  │ Model      │  │ Fallback  │  │ Load   │ │
│  │ Router     │  │ Handler   │  │ Balancer│ │
│  └─────┬──────┘  └─────┬─────┘  └───┬────┘ │
│        │               │            │       │
│  ┌─────┴───────────────┴────────────┴────┐  │
│  │         Provider Selection             │  │
│  └─────────────┬──────────────┬──────────┘  │
│                │              │              │
│  ┌─────────────▼──┐  ┌───────▼───────────┐ │
│  │  GroqApiClient  │  │  GlmApiClient     │ │
│  │  (Primary)      │  │  (Secondary)      │ │
│  └────────────────┘  └──────────────────┘  │
│                                              │
│  ┌──────────────────────────────────────┐   │
│  │  Usage Tracker & Cost Monitor        │   │
│  └──────────────────────────────────────┘   │
└─────────────────────────────────────────────┘
```

---

## Proposed Changes

### Component 1: Provider Abstraction

The current system has `GroqApiClient` as a concrete class injected directly into `BaseAgent`. We need to abstract this so both Groq and GLM can be used interchangeably.

---

#### [NEW] [IAiProviderClient.cs](file:///d:/LAGBAR%20SHOP/OnlineStore_updated/OnlineStore/backend/OnlineStore.Api/Services/Agents/IAiProviderClient.cs)

Common interface for all AI providers. Both `GroqApiClient` and `GlmApiClient` implement this.

```csharp
public interface IAiProviderClient
{
    /// <summary>Provider name for logging and tracking (e.g., "groq", "glm").</summary>
    string ProviderName { get; }

    Task<ChatCompletionResult> SendChatCompletionAsync(
        string model,
        List<GroqMessageDto> messages,
        List<GroqToolDto>? tools = null,
        CancellationToken cancellationToken = default);

    Task<ChatCompletionResult> SendChatCompletionStreamingAsync(
        string model,
        List<GroqMessageDto> messages,
        List<GroqToolDto>? tools,
        Func<string, CancellationToken, Task> onToken,
        CancellationToken cancellationToken = default);
}
```

#### [MODIFY] [GroqApiClient.cs](file:///d:/LAGBAR%20SHOP/OnlineStore_updated/OnlineStore/backend/OnlineStore.Api/Services/Agents/GroqApiClient.cs)

- Implement `IAiProviderClient`
- Add `public string ProviderName => "groq";`
- **On 429 response**: instead of throwing immediately, throw a custom `RateLimitException` that the gateway can catch and retry with GLM

```diff
-public class GroqApiClient
+public class GroqApiClient : IAiProviderClient
 {
+    public string ProviderName => "groq";
```

---

#### [NEW] [GlmApiClient.cs](file:///d:/LAGBAR%20SHOP/OnlineStore_updated/OnlineStore/backend/OnlineStore.Api/Services/Agents/GlmApiClient.cs)

GLM client using the **OpenAI-compatible** `https://open.bigmodel.cn/api/paas/v4/` endpoint. Since GLM uses the same request/response format as OpenAI, this class is nearly identical to `GroqApiClient` — same SSE parsing, same tool call delta merging.

```csharp
public class GlmApiClient : IAiProviderClient
{
    public string ProviderName => "glm";
    
    // Constructor receives HttpClient configured with:
    //   BaseAddress = "https://open.bigmodel.cn/api/paas/v4/"
    //   Authorization = "Bearer {GLM_API_KEY}"
    
    // SendChatCompletionAsync — same logic as GroqApiClient
    // SendChatCompletionStreamingAsync — same logic as GroqApiClient
}
```

> [!TIP]
> Since both clients share 95% of the same code, the implementation extracts the shared HTTP/SSE logic into a `private` base method. Only the error handling and provider-specific headers differ.

---

### Component 2: AI Gateway (Central Router)

The core of the multi-provider strategy. Every AI request flows through this gateway.

---

#### [NEW] [AiGateway.cs](file:///d:/LAGBAR%20SHOP/OnlineStore_updated/OnlineStore/backend/OnlineStore.Api/Services/Agents/AiGateway.cs)

```csharp
public class AiGateway
{
    private readonly GroqApiClient _groqClient;
    private readonly GlmApiClient _glmClient;
    private readonly AiUsageTracker _usageTracker;
    private readonly ILogger<AiGateway> _logger;
    private readonly AiProviderConfig _config;
    
    /// <summary>
    /// Send a chat completion through the gateway.
    /// The gateway selects the provider + model, handles fallback, and tracks usage.
    /// </summary>
    public Task<ChatCompletionResult> SendAsync(
        AiRequestContext requestContext,
        List<GroqMessageDto> messages,
        List<GroqToolDto>? tools = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Send a streaming chat completion through the gateway.
    /// </summary>
    public Task<ChatCompletionResult> SendStreamingAsync(
        AiRequestContext requestContext,
        List<GroqMessageDto> messages,
        List<GroqToolDto>? tools,
        Func<string, CancellationToken, Task> onToken,
        CancellationToken ct = default);
}
```

**Internal flow:**

```
1. Receive request with AiRequestContext (agentName, taskComplexity)
    │
2. Model Router: map taskComplexity → (provider, model)
    │
3. Load Balancer: check if Groq is above threshold
    │  → If yes and model has GLM equivalent, route to GLM
    │
4. Execute request on selected provider
    │
5. On 429 (rate limit):
    │  → Automatic fallback to GLM with equivalent model
    │  → Log fallback event
    │
6. Track usage (provider, model, tokens, latency)
    │
7. Return result
```

---

#### [NEW] [AiRequestContext.cs](file:///d:/LAGBAR%20SHOP/OnlineStore_updated/OnlineStore/backend/OnlineStore.Api/Services/Agents/AiRequestContext.cs)

Metadata that travels with each AI request so the gateway can make routing decisions.

```csharp
public class AiRequestContext
{
    /// <summary>Which agent is making the request (e.g., "shopping", "planner").</summary>
    public string AgentName { get; init; } = "";
    
    /// <summary>Task complexity determines model selection.</summary>
    public TaskComplexity Complexity { get; init; } = TaskComplexity.Medium;
    
    /// <summary>Optional: force a specific provider (bypass routing).</summary>
    public string? ForceProvider { get; init; }
    
    /// <summary>Optional: force a specific model (bypass routing).</summary>
    public string? ForceModel { get; init; }
}

public enum TaskComplexity
{
    /// <summary>Simple greetings, FAQ, quick lookups → Compound Mini</summary>
    Simple,
    
    /// <summary>Product search, standard assistance → Qwen3-27B</summary>
    Medium,
    
    /// <summary>Multi-step planning, complex shopping → GPT-OSS-120B</summary>
    Complex,
    
    /// <summary>Reports, descriptions, translation, background → GLM 5.2</summary>
    Background
}
```

---

### Component 3: Model Routing Table

The gateway uses a routing table to map `TaskComplexity` → `(provider, model)`:

| TaskComplexity | Primary Provider | Primary Model | Fallback Model |
|----------------|-----------------|---------------|----------------|
| `Simple` | Groq | `compound-mini` | `glm-5.2` |
| `Medium` | Groq | `qwen3-27b` | `glm-5.2` |
| `Complex` | Groq | `gpt-oss-120b` | `glm-5.2` |
| `Background` | GLM | `glm-5.2` | *(none — GLM is primary)* |

**Agent → TaskComplexity mapping:**

| Agent | TaskComplexity | Rationale |
|-------|---------------|-----------|
| `PlannerAgent` | Complex | Multi-step intent classification |
| `ShopOwnerPlannerAgent` | Complex | Multi-step intent classification |
| `AdminPlannerAgent` | Complex | Multi-step intent classification |
| `ShoppingAgent` | Medium | Product search reasoning |
| `CustomerServiceAgent` | Medium | Customer support |
| `RecommendationAgent` | Medium | Product recommendation |
| `OrderTrackingAgent` | Simple | Simple status lookups |
| `GeneralChatAgent` | Simple | Greetings, FAQ |
| `ReflectionAgent` | Background | Metacognition review |
| `ProductManagementAgent` | Medium | Product CRUD |
| `ReportingAgent` | Background | Report generation |
| `AnalyticsAgent` | Medium | Business analytics |
| `InventoryAgent` | Medium | Inventory analysis |
| `MarketingAgent` | Background | Marketing content |
| `CustomerInsightAgent` | Medium | Customer analysis |
| `FraudDetectionAgent` | Complex | Pattern analysis |
| `SecurityMonitoringAgent` | Medium | Security events |
| `PlatformAnalyticsAgent` | Medium | Platform metrics |
| `StoreApprovalAgent` | Medium | Risk assessment |
| `ModerationAgent` | Medium | Content moderation |
| `GlobalSupportAgent` | Medium | Support resolution |

---

### Component 4: Each Agent Declares Its Complexity

#### [MODIFY] [IAgent.cs](file:///d:/LAGBAR%20SHOP/OnlineStore_updated/OnlineStore/backend/OnlineStore.Api/Services/Agents/IAgent.cs)

Add a `TaskComplexity` property:

```diff
 public interface IAgent
 {
     string AgentName { get; }
     string SystemPrompt { get; }
+    TaskComplexity DefaultComplexity { get; }
```

#### [MODIFY] [BaseAgent.cs](file:///d:/LAGBAR%20SHOP/OnlineStore_updated/OnlineStore/backend/OnlineStore.Api/Services/Agents/BaseAgent.cs)

Major changes:
1. Replace `GroqApiClient` dependency with `AiGateway`
2. Remove `DefaultModel` constant — the gateway handles model selection
3. In `RunAgentLoopAsync`, build `AiRequestContext` from `AgentName` + `DefaultComplexity`
4. Call `_gateway.SendAsync()` / `_gateway.SendStreamingAsync()` instead of `GroqClient` directly

```diff
 public abstract class BaseAgent : IAgent
 {
-    protected const string DefaultModel = "llama-3.3-70b-versatile";
-    protected readonly GroqApiClient GroqClient;
+    protected readonly AiGateway Gateway;
 
+    public virtual TaskComplexity DefaultComplexity => TaskComplexity.Medium;
 
     protected BaseAgent(
-        GroqApiClient groqClient,
+        AiGateway gateway,
         ToolRegistry toolRegistry,
         IChatHistoryService chatHistory,
         ILogger logger)
     {
-        GroqClient = groqClient;
+        Gateway = gateway;
```

In the loop:

```diff
-    iterationResult = await GroqClient.SendChatCompletionAsync(
-        DefaultModel, messages, tools.Count > 0 ? tools : null, cancellationToken);
+    var aiContext = new AiRequestContext
+    {
+        AgentName = AgentName,
+        Complexity = DefaultComplexity
+    };
+    iterationResult = await Gateway.SendAsync(
+        aiContext, messages, tools.Count > 0 ? tools : null, cancellationToken);
```

#### [MODIFY] Each Agent Constructor

All agents that inject `GroqApiClient` need to inject `AiGateway` instead. The constructor signature changes from:

```diff
- public ShoppingAgent(GroqApiClient groqClient, ToolRegistry toolRegistry, ...)
-     : base(groqClient, toolRegistry, ...)
+ public ShoppingAgent(AiGateway gateway, ToolRegistry toolRegistry, ...)
+     : base(gateway, toolRegistry, ...)
```

And override `DefaultComplexity` where the default (`Medium`) is not correct:

```csharp
// In GeneralChatAgent:
public override TaskComplexity DefaultComplexity => TaskComplexity.Simple;

// In ReflectionAgent:
public override TaskComplexity DefaultComplexity => TaskComplexity.Background;

// In PlannerAgent:
public override TaskComplexity DefaultComplexity => TaskComplexity.Complex;
```

---

### Component 5: Automatic Fallback

#### [NEW] [RateLimitException.cs](file:///d:/LAGBAR%20SHOP/OnlineStore_updated/OnlineStore/backend/OnlineStore.Api/Services/Agents/RateLimitException.cs)

```csharp
public class RateLimitException : HttpRequestException
{
    public string ProviderName { get; }
    public TimeSpan? RetryAfter { get; }
    
    public RateLimitException(string providerName, TimeSpan? retryAfter = null)
        : base($"Rate limit reached on {providerName}.")
    {
        ProviderName = providerName;
        RetryAfter = retryAfter;
    }
}
```

**Fallback logic inside `AiGateway`:**

```csharp
try
{
    return await primaryClient.SendChatCompletionAsync(primaryModel, messages, tools, ct);
}
catch (RateLimitException ex) when (ex.ProviderName == "groq")
{
    _logger.LogWarning("Groq rate limited. Falling back to GLM 5.2");
    _usageTracker.RecordFallback("groq", "glm");
    return await _glmClient.SendChatCompletionAsync("glm-5.2", messages, tools, ct);
}
```

---

### Component 6: Load Balancing

#### [NEW] [AiUsageTracker.cs](file:///d:/LAGBAR%20SHOP/OnlineStore_updated/OnlineStore/backend/OnlineStore.Api/Services/Agents/AiUsageTracker.cs)

A **singleton** that tracks per-provider request counts using a sliding window:

```csharp
public class AiUsageTracker
{
    // Sliding window: count requests in the last 60 seconds
    private readonly ConcurrentDictionary<string, SlidingWindowCounter> _providerCounters;
    
    /// <summary>Should we pre-emptively route away from this provider?</summary>
    public bool IsAboveThreshold(string providerName)
    {
        // If Groq has > N requests/min, return true
        if (_providerCounters.TryGetValue(providerName, out var counter))
            return counter.Count > _config.GroqThresholdPerMinute;  // e.g., 25
        return false;
    }
    
    public void RecordRequest(string provider, string model, int tokens, long latencyMs);
    public void RecordFallback(string fromProvider, string toProvider);
    
    /// <summary>Get usage summary for monitoring.</summary>
    public AiUsageSummary GetSummary();
}
```

**Load balancing logic in `AiGateway`:**

```csharp
// Before sending to primary provider:
if (primaryProvider == "groq" && _usageTracker.IsAboveThreshold("groq"))
{
    _logger.LogInformation("Groq above threshold. Pre-emptively routing to GLM.");
    return await _glmClient.SendChatCompletionAsync("glm-5.2", messages, tools, ct);
}
```

---

### Component 7: Configuration

#### [MODIFY] [appsettings.json](file:///d:/LAGBAR%20SHOP/OnlineStore_updated/OnlineStore/backend/OnlineStore.Api/appsettings.json)

```json
{
    "Groq": {
        "ApiKey": "***REMOVED***"
    },
    "Glm": {
        "ApiKey": "***REMOVED***",
        "BaseUrl": "https://open.bigmodel.cn/api/paas/v4/"
    },
    "AiGateway": {
        "GroqThresholdPerMinute": 25,
        "EnableLoadBalancing": true,
        "EnableFallback": true,
        "DefaultFallbackModel": "glm-5.2",
        "ModelRouting": {
            "Simple": { "Provider": "groq", "Model": "compound-mini" },
            "Medium": { "Provider": "groq", "Model": "qwen3-27b" },
            "Complex": { "Provider": "groq", "Model": "gpt-oss-120b" },
            "Background": { "Provider": "glm", "Model": "glm-5.2" }
        }
    }
}
```

#### [NEW] [AiProviderConfig.cs](file:///d:/LAGBAR%20SHOP/OnlineStore_updated/OnlineStore/backend/OnlineStore.Api/Services/Agents/AiProviderConfig.cs)

Strongly-typed configuration class bound from `appsettings.json`:

```csharp
public class AiProviderConfig
{
    public int GroqThresholdPerMinute { get; set; } = 25;
    public bool EnableLoadBalancing { get; set; } = true;
    public bool EnableFallback { get; set; } = true;
    public string DefaultFallbackModel { get; set; } = "glm-5.2";
    public Dictionary<string, ModelRoute> ModelRouting { get; set; } = new();
}

public class ModelRoute
{
    public string Provider { get; set; } = "groq";
    public string Model { get; set; } = "qwen3-27b";
}
```

---

### Component 8: DI Registration

#### [MODIFY] [Program.cs](file:///d:/LAGBAR%20SHOP/OnlineStore_updated/OnlineStore/backend/OnlineStore.Api/Program.cs)

```csharp
// ═══════════════════════════════════════════════════
//  Multi-Provider AI Gateway
// ═══════════════════════════════════════════════════

// Configuration
builder.Services.Configure<AiProviderConfig>(
    builder.Configuration.GetSection("AiGateway"));

// Groq API Client (existing — add IAiProviderClient interface)
builder.Services.AddHttpClient<GroqApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
    var apiKey = builder.Configuration["Groq:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", apiKey);
});

// GLM API Client (NEW)
builder.Services.AddHttpClient<GlmApiClient>(client =>
{
    var baseUrl = builder.Configuration["Glm:BaseUrl"] 
        ?? "https://open.bigmodel.cn/api/paas/v4/";
    client.BaseAddress = new Uri(baseUrl);
    var apiKey = builder.Configuration["Glm:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", apiKey);
});

// Usage Tracker (Singleton — shared counter state)
builder.Services.AddSingleton<AiUsageTracker>();

// AI Gateway (Scoped — one per request)
builder.Services.AddScoped<AiGateway>();
```

> [!IMPORTANT]
> **Remove** the old `GroqApiClient` registration from the agent constructors. Agents now inject `AiGateway` instead. The `GroqApiClient` is still registered in DI but is only consumed internally by `AiGateway`.

---

## Summary of All Files

### New Files (8)

| File | Purpose |
|------|---------|
| `IAiProviderClient.cs` | Common provider interface |
| `GlmApiClient.cs` | GLM provider (OpenAI-compatible) |
| `AiGateway.cs` | Central router with fallback + load balancing |
| `AiRequestContext.cs` | Request metadata + `TaskComplexity` enum |
| `AiProviderConfig.cs` | Strongly-typed config |
| `AiUsageTracker.cs` | Sliding window usage counter |
| `RateLimitException.cs` | Custom exception for fallback handling |

### Modified Files (7+)

| File | Changes |
|------|---------|
| `GroqApiClient.cs` | Implement `IAiProviderClient`, throw `RateLimitException` on 429 |
| `IAgent.cs` | Add `DefaultComplexity` property |
| `BaseAgent.cs` | Replace `GroqApiClient` → `AiGateway`, remove `DefaultModel` |
| `Program.cs` | Add GLM client, AiGateway, AiUsageTracker, config binding |
| `appsettings.json` | Add `Glm` and `AiGateway` sections |
| `appsettings.Development.json` | Add `Glm` and `AiGateway` sections |
| **All 20+ agent constructors** | Change `GroqApiClient` → `AiGateway` injection |

---

## Open Questions

> [!WARNING]
> **GLM tool calling support.** GLM 5.2 supports function calling via the OpenAI-compatible API, but its tool call format may have minor differences. The implementation should include integration testing with GLM's actual tool call responses to ensure delta merging works correctly. If issues arise, the fallback can disable tool calling for GLM and only use it for text-only tasks.

> [!IMPORTANT]
> **Model names on Groq.** The exact model identifiers on Groq (e.g., `compound-mini`, `qwen3-27b`, `gpt-oss-120b`) need to be verified against the current Groq API model list. The routing table in `appsettings.json` makes this easy to update without code changes.

> [!NOTE]
> **No database migration required.** This is a pure service-layer change. Usage tracking is in-memory (via `AiUsageTracker` singleton). If persistent usage statistics are needed later, a `AiUsageLog` table can be added.

> [!NOTE]
> **The GLM API key** (`***REMOVED***`) provided by the user will be stored in `appsettings.json`. For production, this should be moved to environment variables or a secrets manager.

---

## Verification Plan

### Automated Tests
```bash
cd d:\LAGBAR SHOP\OnlineStore_updated\OnlineStore\backend

# Build — verify all agents compile with AiGateway
dotnet build OnlineStore.Api\OnlineStore.Api.csproj
```

### Manual Verification
1. **Groq routing** — Send a simple customer question → verify it uses `compound-mini`
2. **Complex routing** — Send a multi-step shopping request → verify it uses `gpt-oss-120b`
3. **Background routing** — Trigger a report generation → verify it uses GLM `glm-5.2`
4. **Fallback test** — Temporarily set `GroqThresholdPerMinute: 0` → verify all requests fallback to GLM
5. **Rate limit test** — Send rapid requests to trigger Groq 429 → verify automatic GLM fallback
6. **Usage tracking** — Check logs for provider/model selection and usage statistics
