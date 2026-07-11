using Microsoft.EntityFrameworkCore;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class AbTestingService : IAbTestingService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AbTestingService> _logger;

    public AbTestingService(ApplicationDbContext db, ILogger<AbTestingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AiPromptVariant?> GetVariantForConversationAsync(string agentName, int conversationId)
    {
        // Check if there is an active experiment variant for this agent
        var activeVariants = await _db.AiPromptVariants
            .Where(v => v.AgentName == agentName && v.IsActive && !v.IsDefault)
            .ToListAsync();

        if (!activeVariants.Any())
            return null;

        var conversation = await _db.AiConversations.FindAsync(conversationId);
        if (conversation == null)
            return null;

        // If conversation already has an assigned variant, return it
        if (conversation.PromptVariantId.HasValue)
        {
            return await _db.AiPromptVariants.FindAsync(conversation.PromptVariantId.Value);
        }

        // Deterministic hash assignment (e.g., traffic split 80/20)
        var hash = Math.Abs((conversationId * 31 + 17) % 100);

        AiPromptVariant? selectedVariant = null;
        double accumulatedTraffic = 0;

        foreach (var variant in activeVariants)
        {
            accumulatedTraffic += variant.TrafficPercentage * 100;
            if (hash < accumulatedTraffic)
            {
                selectedVariant = variant;
                break;
            }
        }

        if (selectedVariant != null)
        {
            conversation.PromptVariantId = selectedVariant.Id;
            selectedVariant.TotalConversations++;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Conversation {ConversationId} assigned to A/B test variant {VariantName} for agent {AgentName}", 
                conversationId, selectedVariant.VariantName, agentName);
        }
        else
        {
            // Default variant gets incremented if defined
            var defaultVariant = await _db.AiPromptVariants
                .FirstOrDefaultAsync(v => v.AgentName == agentName && v.IsDefault && v.IsActive);
            
            if (defaultVariant != null)
            {
                conversation.PromptVariantId = defaultVariant.Id;
                defaultVariant.TotalConversations++;
                await _db.SaveChangesAsync();
            }
        }

        return selectedVariant;
    }

    public async Task RecordOutcomeAsync(int conversationId, bool successful)
    {
        var conversation = await _db.AiConversations.FindAsync(conversationId);
        if (conversation == null || !conversation.PromptVariantId.HasValue)
            return;

        var variant = await _db.AiPromptVariants.FindAsync(conversation.PromptVariantId.Value);
        if (variant == null)
            return;

        variant.TotalOutcomes++;
        if (successful)
        {
            variant.SuccessfulOutcomes++;
        }

        // Update conversion rate
        if (variant.TotalOutcomes > 0)
        {
            variant.ConversionRate = Math.Round((double)variant.SuccessfulOutcomes * 100.0 / variant.TotalOutcomes, 2);
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Recorded outcome for variant {VariantName} in conversation {ConversationId}: Success={Successful}", 
            variant.VariantName, conversationId, successful);
    }

    public async Task<object> GetExperimentResultsAsync(string experimentName)
    {
        var variants = await _db.AiPromptVariants
            .Where(v => v.ExperimentName == experimentName)
            .ToListAsync();

        if (!variants.Any())
            return new { Error = $"Experiment {experimentName} not found." };

        return new
        {
            ExperimentName = experimentName,
            TotalConversations = variants.Sum(v => v.TotalConversations),
            Variants = variants.Select(v => new
            {
                v.VariantName,
                v.IsDefault,
                v.IsActive,
                v.TrafficPercentage,
                v.TotalConversations,
                v.TotalOutcomes,
                v.SuccessfulOutcomes,
                v.ConversionRate,
                v.AverageSatisfaction,
                v.WinnerDecision
            })
        };
    }

    public async Task<List<object>> GetActiveExperimentsAsync()
    {
        var activeExperiments = await _db.AiPromptVariants
            .Where(v => v.IsActive && !string.IsNullOrEmpty(v.ExperimentName))
            .GroupBy(v => v.ExperimentName!)
            .Select(g => new
            {
                ExperimentName = g.Key,
                AgentName = g.First().AgentName,
                StartedAt = g.Min(v => v.ExperimentStartedAtUtc),
                TotalVariants = g.Count(),
                TotalConversations = g.Sum(v => v.TotalConversations)
            })
            .Cast<object>()
            .ToListAsync();

        return activeExperiments;
    }

    public async Task CreateExperimentAsync(string experimentName, string agentName, string variantPrompt, double trafficPercentage = 0.2)
    {
        // Verify default variant exists
        var defaultVariant = await _db.AiPromptVariants
            .FirstOrDefaultAsync(v => v.AgentName == agentName && v.IsDefault && v.IsActive);

        if (defaultVariant == null)
        {
            // Auto-create a default variant if none exists yet, using the Agent's standard prompt configuration
            defaultVariant = new AiPromptVariant
            {
                AgentName = agentName,
                VariantName = "Default",
                SystemPrompt = "", // Injected dynamically if blank
                IsActive = true,
                IsDefault = true,
                ExperimentName = experimentName,
                TrafficPercentage = 1.0 - trafficPercentage,
                ExperimentStartedAtUtc = DateTime.UtcNow
            };
            _db.AiPromptVariants.Add(defaultVariant);
        }
        else
        {
            defaultVariant.ExperimentName = experimentName;
            defaultVariant.TrafficPercentage = 1.0 - trafficPercentage;
        }

        var newVariant = new AiPromptVariant
        {
            AgentName = agentName,
            VariantName = $"Variant_{DateTime.UtcNow:yyyyMMdd_HHmmss}",
            SystemPrompt = variantPrompt,
            IsActive = true,
            IsDefault = false,
            ExperimentName = experimentName,
            TrafficPercentage = trafficPercentage,
            ExperimentStartedAtUtc = DateTime.UtcNow
        };

        _db.AiPromptVariants.Add(newVariant);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created new experiment {ExperimentName} for agent {AgentName} with traffic split: Default={DefaultSplit}%, Variant={VariantSplit}%",
            experimentName, agentName, (1.0 - trafficPercentage) * 100, trafficPercentage * 100);
    }

    public async Task EndExperimentAsync(string experimentName, string winnerVariantName)
    {
        var variants = await _db.AiPromptVariants
            .Where(v => v.ExperimentName == experimentName)
            .ToListAsync();

        foreach (var variant in variants)
        {
            variant.IsActive = false;
            variant.ExperimentEndedAtUtc = DateTime.UtcNow;
            if (variant.VariantName == winnerVariantName)
            {
                variant.WinnerDecision = "winner";
                
                // If the winner is a test variant, we should promote its system prompt to be the default one for future runs
                if (!variant.IsDefault)
                {
                    _logger.LogInformation("Promoting variant {Winner} as the new default for agent {AgentName}", winnerVariantName, variant.AgentName);
                    // Deactivate old default variant
                    var oldDefault = variants.FirstOrDefault(v => v.IsDefault);
                    if (oldDefault != null)
                    {
                        oldDefault.IsDefault = false;
                    }
                    variant.IsDefault = true;
                    variant.IsActive = true; // Stay active as the default
                }
            }
            else
            {
                variant.WinnerDecision = "loser";
            }
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Ended experiment {ExperimentName} with winner {Winner}", experimentName, winnerVariantName);
    }
}
