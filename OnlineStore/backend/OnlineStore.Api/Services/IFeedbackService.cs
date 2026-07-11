namespace OnlineStore.Api.Services;

public interface IFeedbackService
{
    Task SubmitExplicitFeedbackAsync(string userId, int conversationId, int messageId, int? rating, string? feedbackType, string? comment);

    Task DetectAndRecordImplicitFeedbackAsync(string userId, int conversationId, string userMessage, string agentName);

    Task<object> GetConversationFeedbackAsync(int conversationId);

    Task<object> GetFeedbackSummaryAsync(DateTime? startDate, DateTime? endDate);
}
