using OnlineStore.Domain.Entities;

namespace OnlineStore.Api.Services;

public interface IAiMemoryService
{
    // ═══════════════════════════════════════════════════
    //  User Preferences (never expire for active users)
    // ═══════════════════════════════════════════════════

    Task<Dictionary<string, string>> GetUserPreferencesAsync(string userId);
    Task UpdatePreferenceAsync(string userId, string key, string value, string source = "inferred", double confidence = 0.5);
    Task InferPreferencesFromConversationAsync(string userId, int conversationId, CancellationToken ct = default);

    // ═══════════════════════════════════════════════════
    //  Shop Context (owner-level business context cache)
    // ═══════════════════════════════════════════════════

    Task<AiShopContext?> GetShopContextAsync(string userId);
    Task SaveShopContextAsync(AiShopContext context);
    Task UpdateShopCustomInstructionsAsync(string userId, string instructions);

    // ═══════════════════════════════════════════════════
    //  Long-Term Memory (facts = never expire, context = 30 day TTL)
    // ═══════════════════════════════════════════════════

    Task<List<AiLongTermMemory>> RecallMemoriesAsync(string userId, string query, int maxResults = 5);
    Task SaveMemoryAsync(string userId, int conversationId, string memoryType, string content, double importance = 0.5);
    Task ExtractAndSaveMemoriesAsync(string userId, int conversationId, string userMessage, string aiResponse, CancellationToken ct = default);
    Task EvictExpiredMemoriesAsync(CancellationToken ct = default);

    // ═══════════════════════════════════════════════════
    //  Episodic Memory (auto-detected important interactions)
    // ═══════════════════════════════════════════════════

    Task<List<AiEpisodicMemory>> GetRelevantEpisodesAsync(string userId, string? episodeType = null, int maxResults = 3);
    Task RecordEpisodeAsync(string userId, int conversationId, string episodeType, string summary, string? outcome = null);
    Task DetectAndRecordEpisodesAsync(string userId, int conversationId, CancellationToken ct = default);

    // ═══════════════════════════════════════════════════
    //  Unified: Build memory context for agent prompts
    // ═══════════════════════════════════════════════════

    Task<string> BuildMemoryContextAsync(string userId, string userMessage, string userRole, CancellationToken ct = default);
}
