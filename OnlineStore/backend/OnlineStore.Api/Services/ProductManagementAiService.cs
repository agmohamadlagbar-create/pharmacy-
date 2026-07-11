using OnlineStore.Api.Contracts;
using OnlineStore.Api.Services.Agents;

namespace OnlineStore.Api.Services;

/// <summary>
/// AI-powered product management operations for shop owners.
/// Uses AiGateway to generate descriptions, SEO keywords, categories, and translations.
/// </summary>
public class ProductManagementAiService : IProductManagementAiService
{
    private readonly AiGateway _gateway;
    private readonly ILogger<ProductManagementAiService> _logger;

    public ProductManagementAiService(AiGateway gateway, ILogger<ProductManagementAiService> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<string> GenerateDescriptionAsync(string productName, string category, string? existingDescription = null)
    {
        var prompt = $"Generate a compelling, SEO-friendly product description for:\n" +
                     $"Product: {productName}\n" +
                     $"Category: {category}\n" +
                     (existingDescription != null ? $"Existing description to improve: {existingDescription}\n" : "") +
                     "Requirements:\n" +
                     "- 2-3 paragraphs\n" +
                     "- Highlight key features and benefits\n" +
                     "- Use persuasive language\n" +
                     "- Include relevant keywords naturally\n" +
                     "Return ONLY the description text, no meta-commentary.";

        var messages = new List<GroqMessageDto>
        {
            new() { Role = "system", Content = "You are a professional e-commerce copywriter." },
            new() { Role = "user", Content = prompt }
        };

        var aiContext = new AiRequestContext { AgentName = "product_management_service", Complexity = TaskComplexity.Medium };
        var result = await _gateway.SendAsync(aiContext, messages, null, default);
        return result.Content;
    }

    public async Task<string> GenerateSeoKeywordsAsync(string productName, string description)
    {
        var prompt = $"Generate 10-15 SEO keywords for this product:\n" +
                     $"Product: {productName}\n" +
                     $"Description: {description}\n\n" +
                     "Return as a comma-separated list. Include:\n" +
                     "- Primary keywords\n" +
                     "- Long-tail keywords\n" +
                     "- Related search terms\n" +
                     "Return ONLY the keywords, no explanations.";

        var messages = new List<GroqMessageDto>
        {
            new() { Role = "system", Content = "You are an SEO specialist for e-commerce." },
            new() { Role = "user", Content = prompt }
        };

        var aiContext = new AiRequestContext { AgentName = "product_management_service", Complexity = TaskComplexity.Medium };
        var result = await _gateway.SendAsync(aiContext, messages, null, default);
        return result.Content;
    }

    public async Task<string> SuggestCategoriesAsync(string productName)
    {
        var prompt = $"Suggest the most appropriate e-commerce categories for: {productName}\n" +
                     "Return as a JSON array of strings, e.g., [\"Food\", \"Snacks\", \"Organic\"]\n" +
                     "Return ONLY the JSON array.";

        var messages = new List<GroqMessageDto>
        {
            new() { Role = "system", Content = "You are an e-commerce category specialist." },
            new() { Role = "user", Content = prompt }
        };

        var aiContext = new AiRequestContext { AgentName = "product_management_service", Complexity = TaskComplexity.Medium };
        var result = await _gateway.SendAsync(aiContext, messages, null, default);
        return result.Content;
    }

    public async Task<string> TranslateProductAsync(string productData, string targetLanguage)
    {
        var prompt = $"Translate the following product information to {targetLanguage}:\n\n{productData}\n\n" +
                     "Maintain the same format and structure. " +
                     "Keep brand names and technical specs in their original form. " +
                     "Return ONLY the translated text.";

        var messages = new List<GroqMessageDto>
        {
            new() { Role = "system", Content = "You are a professional product translator specializing in e-commerce." },
            new() { Role = "user", Content = prompt }
        };

        var aiContext = new AiRequestContext { AgentName = "product_management_service", Complexity = TaskComplexity.Medium };
        var result = await _gateway.SendAsync(aiContext, messages, null, default);
        return result.Content;
    }
}
