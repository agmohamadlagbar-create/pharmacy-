using Microsoft.EntityFrameworkCore;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;
using System.Text.Json;

namespace OnlineStore.Api.Services;

public class AiMemoryService : IAiMemoryService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AiMemoryService> _logger;

    public AiMemoryService(ApplicationDbContext db, ILogger<AiMemoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════
    //  User Preferences
    // ═══════════════════════════════════════════════════

    public async Task<Dictionary<string, string>> GetUserPreferencesAsync(string userId)
    {
        return await _db.AiUserPreferences
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.PreferenceKey, p => p.PreferenceValue);
    }

    public async Task UpdatePreferenceAsync(string userId, string key, string value, string source = "inferred", double confidence = 0.5)
    {
        var preference = await _db.AiUserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.PreferenceKey == key);

        if (preference == null)
        {
            preference = new AiUserPreference
            {
                UserId = userId,
                PreferenceKey = key,
                PreferenceValue = value,
                Source = source,
                Confidence = confidence,
                OccurrenceCount = 1
            };
            _db.AiUserPreferences.Add(preference);
        }
        else
        {
            if (preference.PreferenceValue == value)
            {
                preference.OccurrenceCount++;
                preference.Confidence = Math.Min(1.0, preference.Confidence + 0.1);
            }
            else
            {
                // Decaying confidence if user switches preference, but update if source is explicit or confidence is low
                if (source == "explicit" || preference.Confidence < 0.4)
                {
                    preference.PreferenceValue = value;
                    preference.Confidence = confidence;
                }
                else
                {
                    preference.Confidence = Math.Max(0.1, preference.Confidence - 0.2);
                }
            }
            preference.Source = source;
            preference.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task InferPreferencesFromConversationAsync(string userId, int conversationId, CancellationToken ct = default)
    {
        try
        {
            var messages = await _db.AiMessages
                .Where(m => m.ConversationId == conversationId && m.Role == "user")
                .OrderByDescending(m => m.SequenceNumber)
                .Take(5)
                .Select(m => m.Content)
                .ToListAsync(ct);

            foreach (var content in messages)
            {
                var text = content.ToLowerInvariant();

                // Language preference inference
                if (text.Contains("merhaba") || text.Contains("teşekkür") || text.Contains("lütfen"))
                {
                    await UpdatePreferenceAsync(userId, "preferred_language", "tr", "inferred");
                }
                else if (text.Contains("مرحبا") || text.Contains("شكرا") || text.Contains("من فضلك"))
                {
                    await UpdatePreferenceAsync(userId, "preferred_language", "ar", "inferred");
                }
                else if (text.Contains("hello") || text.Contains("thank you") || text.Contains("please"))
                {
                    await UpdatePreferenceAsync(userId, "preferred_language", "en", "inferred");
                }

                // Category preferences inference
                if (text.Contains("phone") || text.Contains("laptop") || text.Contains("computer") || text.Contains("electronic"))
                {
                    await UpdatePreferenceAsync(userId, "interest_category", "electronics", "inferred", 0.6);
                }
                else if (text.Contains("dress") || text.Contains("shirt") || text.Contains("shoes") || text.Contains("clothing"))
                {
                    await UpdatePreferenceAsync(userId, "interest_category", "clothing", "inferred", 0.6);
                }

                // Price sensitivity inference
                if (text.Contains("cheap") || text.Contains("discount") || text.Contains("best price") || text.Contains("ucuz") || text.Contains("indirim"))
                {
                    await UpdatePreferenceAsync(userId, "price_sensitivity", "high", "inferred", 0.7);
                }
                else if (text.Contains("premium") || text.Contains("luxury") || text.Contains("best quality") || text.Contains("kaliteli"))
                {
                    await UpdatePreferenceAsync(userId, "price_sensitivity", "low", "inferred", 0.6);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inferring preferences for user {UserId} in conversation {ConversationId}", userId, conversationId);
        }
    }

    // ═══════════════════════════════════════════════════
    //  Shop Context
    // ═══════════════════════════════════════════════════

    public async Task<AiShopContext?> GetShopContextAsync(string userId)
    {
        return await _db.AiShopContexts.FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task SaveShopContextAsync(AiShopContext context)
    {
        var existing = await _db.AiShopContexts.FirstOrDefaultAsync(s => s.UserId == context.UserId);
        if (existing == null)
        {
            _db.AiShopContexts.Add(context);
        }
        else
        {
            existing.PreferredLanguage = context.PreferredLanguage;
            existing.PreferredCurrency = context.PreferredCurrency;
            existing.ShopPoliciesJson = context.ShopPoliciesJson;
            existing.ReportingPreferencesJson = context.ReportingPreferencesJson;
            existing.CustomInstructionsJson = context.CustomInstructionsJson;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
    }

    public async Task UpdateShopCustomInstructionsAsync(string userId, string instructions)
    {
        var context = await _db.AiShopContexts.FirstOrDefaultAsync(s => s.UserId == userId);
        if (context != null)
        {
            context.CustomInstructionsJson = JsonSerializer.Serialize(new { customInstructions = instructions });
            context.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    // ═══════════════════════════════════════════════════
    //  Long-Term Memory
    // ═══════════════════════════════════════════════════

    public async Task<List<AiLongTermMemory>> RecallMemoriesAsync(string userId, string query, int maxResults = 5)
    {
        var now = DateTime.UtcNow;
        var memories = await _db.AiLongTermMemories
            .Where(m => m.UserId == userId && (m.ExpiresAtUtc == null || m.ExpiresAtUtc > now))
            .ToListAsync();

        var queryWords = query.ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);

        var ratedMemories = memories.Select(m =>
        {
            var contentLower = m.Content.ToLowerInvariant();
            var matches = queryWords.Count(qw => contentLower.Contains(qw));
            double relevance = queryWords.Length > 0 ? (double)matches / queryWords.Length : 0.0;

            // Score combines Jaccard similarity, base importance, and access count/recency
            double score = (relevance * 0.6) + (m.Importance * 0.4);
            return new { Memory = m, Score = score };
        })
        .Where(x => x.Score > 0.1 || queryWords.Length == 0) // If query is empty, return top important memories
        .OrderByDescending(x => x.Score)
        .Take(maxResults)
        .Select(x => x.Memory)
        .ToList();

        foreach (var m in ratedMemories)
        {
            m.AccessCount++;
            m.LastAccessedAtUtc = now;
        }

        if (ratedMemories.Any())
        {
            await _db.SaveChangesAsync();
        }

        return ratedMemories;
    }

    public async Task SaveMemoryAsync(string userId, int conversationId, string memoryType, string content, double importance = 0.5)
    {
        var existing = await _db.AiLongTermMemories
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Content == content);

        if (existing == null)
        {
            var memory = new AiLongTermMemory
            {
                UserId = userId,
                SourceConversationId = conversationId,
                MemoryType = memoryType,
                Content = content,
                Importance = importance,
                ExpiresAtUtc = memoryType == "context" ? DateTime.UtcNow.AddDays(30) : null
            };
            _db.AiLongTermMemories.Add(memory);
        }
        else
        {
            existing.LastAccessedAtUtc = DateTime.UtcNow;
            existing.AccessCount++;
            existing.Importance = Math.Min(1.0, existing.Importance + 0.05);
        }

        await _db.SaveChangesAsync();
    }

    public async Task ExtractAndSaveMemoriesAsync(string userId, int conversationId, string userMessage, string aiResponse, CancellationToken ct = default)
    {
        try
        {
            var message = userMessage.ToLowerInvariant();
            // Simple rule-based extraction to remember key details (e.g. birthday, wedding, business use)
            if ((message.Contains("birthday") || message.Contains("doğum günü") || message.Contains("عيد ميلاد")) && 
                (message.Contains("gift") || message.Contains("hediye")))
            {
                await SaveMemoryAsync(userId, conversationId, "context", "Buying a birthday gift.", 0.6);
            }
            else if (message.Contains("wedding") || message.Contains("düğün") || message.Contains("زفاف"))
            {
                await SaveMemoryAsync(userId, conversationId, "context", "Shopping/planning for a wedding.", 0.7);
            }
            else if (message.Contains("my child") || message.Contains("my son") || message.Contains("my daughter") || message.Contains("oğlum") || message.Contains("kızım"))
            {
                await SaveMemoryAsync(userId, conversationId, "relationship", "Has child/family member they shop for.", 0.5);
            }
            else if (message.Contains("allergy") || message.Contains("allergic") || message.Contains("alerji"))
            {
                await SaveMemoryAsync(userId, conversationId, "fact", $"Allergy constraints mentioned in: '{userMessage}'", 0.9);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting memories for user {UserId} in conversation {ConversationId}", userId, conversationId);
        }
    }

    public async Task EvictExpiredMemoriesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var expired = await _db.AiLongTermMemories
            .Where(m => m.ExpiresAtUtc != null && m.ExpiresAtUtc < now)
            .ToListAsync(ct);

        if (expired.Any())
        {
            _logger.LogInformation("Evicting {Count} expired short-term/context AI memories", expired.Count);
            _db.AiLongTermMemories.RemoveRange(expired);
            await _db.SaveChangesAsync(ct);
        }
    }

    // ═══════════════════════════════════════════════════
    //  Episodic Memory
    // ═══════════════════════════════════════════════════

    public async Task<List<AiEpisodicMemory>> GetRelevantEpisodesAsync(string userId, string? episodeType = null, int maxResults = 3)
    {
        var query = _db.AiEpisodicMemories
            .Where(e => e.UserId == userId);

        if (!string.IsNullOrEmpty(episodeType))
        {
            query = query.Where(e => e.EpisodeType == episodeType);
        }

        return await query
            .OrderByDescending(e => e.Importance)
            .ThenByDescending(e => e.EpisodeDateUtc)
            .Take(maxResults)
            .ToListAsync();
    }

    public async Task RecordEpisodeAsync(string userId, int conversationId, string episodeType, string summary, string? outcome = null)
    {
        var episode = new AiEpisodicMemory
        {
            UserId = userId,
            ConversationId = conversationId,
            EpisodeType = episodeType,
            Summary = summary,
            Outcome = outcome,
            Importance = episodeType == "complaint_resolved" || episodeType == "refund_processed" ? 0.8 : 0.6
        };

        _db.AiEpisodicMemories.Add(episode);
        await _db.SaveChangesAsync();
    }

    public async Task DetectAndRecordEpisodesAsync(string userId, int conversationId, CancellationToken ct = default)
    {
        try
        {
            // Check if there was a refund processed or complaint mentioned in the last messages of the conversation
            var messages = await _db.AiMessages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SequenceNumber)
                .Take(6)
                .ToListAsync(ct);

            var textDump = string.Join("\n", messages.Select(m => m.Content.ToLowerInvariant()));

            if (textDump.Contains("refund") || textDump.Contains("iade") || textDump.Contains("money back"))
            {
                var outcome = textDump.Contains("succeeded") || textDump.Contains("approved") || textDump.Contains("completed") ? "resolved" : "pending";
                await RecordEpisodeAsync(userId, conversationId, "refund_processed", "Discussed or processed a product refund.", outcome);
            }
            else if (textDump.Contains("complain") || textDump.Contains("broken") || textDump.Contains("damaged") || textDump.Contains("wrong product") || textDump.Contains("şikayet"))
            {
                await RecordEpisodeAsync(userId, conversationId, "complaint_resolved", "Customer raised an issue or complaint about order or service.", "resolved");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting episodes for user {UserId} in conversation {ConversationId}", userId, conversationId);
        }
    }

    // ═══════════════════════════════════════════════════
    //  Unified Memory Injector
    // ═══════════════════════════════════════════════════

    public async Task<string> BuildMemoryContextAsync(string userId, string userMessage, string userRole, CancellationToken ct = default)
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== [System Memory Context] ===");

            // 1. User Preferences
            var preferences = await GetUserPreferencesAsync(userId);
            if (preferences.Any())
            {
                sb.AppendLine("User Preferences:");
                foreach (var pref in preferences)
                {
                    sb.AppendLine($"- {pref.Key}: {pref.Value}");
                }
            }

            // 2. Shop Context (if owner)
            if (userRole == "Shop" || userRole == "Admin")
            {
                var shopContext = await GetShopContextAsync(userId);
                if (shopContext != null)
                {
                    sb.AppendLine("Shop Owner Preferences & Context:");
                    if (!string.IsNullOrEmpty(shopContext.PreferredCurrency))
                        sb.AppendLine($"- Preferred Currency: {shopContext.PreferredCurrency}");
                    if (!string.IsNullOrEmpty(shopContext.ReportingPreferencesJson))
                        sb.AppendLine($"- Reporting Preferences: {shopContext.ReportingPreferencesJson}");
                    if (!string.IsNullOrEmpty(shopContext.CustomInstructionsJson))
                    {
                        var opts = JsonSerializer.Deserialize<JsonElement>(shopContext.CustomInstructionsJson);
                        if (opts.TryGetProperty("customInstructions", out var inst))
                            sb.AppendLine($"- Custom Owner Instruction: {inst.GetString()}");
                    }
                }
            }

            // 3. Relevant Long-Term Memories
            var memories = await RecallMemoriesAsync(userId, userMessage, 3);
            if (memories.Any())
            {
                sb.AppendLine("Recalled Facts:");
                foreach (var mem in memories)
                {
                    sb.AppendLine($"- {mem.Content}");
                }
            }

            // 4. Episodic Memories
            var episodes = await GetRelevantEpisodesAsync(userId, null, 2);
            if (episodes.Any())
            {
                sb.AppendLine("Relevant Past Episodes:");
                foreach (var ep in episodes)
                {
                    sb.AppendLine($"- [{ep.EpisodeType}] {ep.Summary} (Outcome: {ep.Outcome ?? "n/a"})");
                }
            }

            sb.AppendLine("===============================");
            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building memory context for user {UserId}", userId);
            return string.Empty;
        }
    }
}
