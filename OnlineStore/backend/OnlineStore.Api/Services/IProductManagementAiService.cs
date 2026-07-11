namespace OnlineStore.Api.Services;

/// <summary>
/// AI-powered product management operations for shop owners.
/// Uses Groq LLM to generate descriptions, SEO keywords, and more.
/// </summary>
public interface IProductManagementAiService
{
    Task<string> GenerateDescriptionAsync(string productName, string category, string? existingDescription = null);
    Task<string> GenerateSeoKeywordsAsync(string productName, string description);
    Task<string> SuggestCategoriesAsync(string productName);
    Task<string> TranslateProductAsync(string productData, string targetLanguage);
}
