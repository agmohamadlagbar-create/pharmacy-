using OnlineStore.Domain.Entities;

namespace OnlineStore.Api.Services;

public interface IAbTestingService
{
    /// <summary>
    /// Get the prompt variant assigned to a conversation (or null if no active experiment).
    /// Uses deterministic hashing for 80/20 traffic split.
    /// </summary>
    Task<AiPromptVariant?> GetVariantForConversationAsync(string agentName, int conversationId);

    /// <summary>
    /// Record a positive or negative outcome for a conversation's A/B test variant.
    /// </summary>
    Task RecordOutcomeAsync(int conversationId, bool successful);

    /// <summary>
    /// Get experiment results including conversion rates and statistical significance.
    /// </summary>
    Task<object> GetExperimentResultsAsync(string experimentName);

    /// <summary>
    /// List all active experiments.
    /// </summary>
    Task<List<object>> GetActiveExperimentsAsync();

    /// <summary>
    /// Create a new A/B experiment for an agent. Default traffic split: 80/20.
    /// </summary>
    Task CreateExperimentAsync(string experimentName, string agentName, string variantPrompt, double trafficPercentage = 0.2);

    /// <summary>
    /// End an experiment and declare a winner.
    /// </summary>
    Task EndExperimentAsync(string experimentName, string winnerVariantName);
}
