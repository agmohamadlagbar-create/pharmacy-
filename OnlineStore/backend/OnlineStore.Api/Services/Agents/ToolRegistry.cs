using System.Text.Json;
using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services.Agents;

/// <summary>
/// A registered tool with its Groq function schema, permission constraints, and executor.
/// </summary>
public class RegisteredTool
{
    public string Name { get; init; } = string.Empty;
    public GroqToolDto Schema { get; init; } = new();
    public HashSet<string> AllowedAgents { get; init; } = new();
    public HashSet<string> AllowedRoles { get; init; } = new();
    public Func<string, string, int, Task<string>> Executor { get; init; } = null!;
}

/// <summary>
/// Centralized tool catalog with permission enforcement.
/// All tool definitions and executors are registered here, and agents
/// query this registry for their authorized tool set.
/// </summary>
public class ToolRegistry
{
    private readonly Dictionary<string, RegisteredTool> _tools = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ToolRegistry> _logger;

    public ToolRegistry(ILogger<ToolRegistry> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register a tool with its schema, allowed agents, allowed roles, and executor function.
    /// The executor receives (arguments JSON, userId, conversationId) and returns a result string.
    /// </summary>
    public void RegisterTool(
        string name,
        GroqToolDto schema,
        HashSet<string> allowedAgents,
        HashSet<string> allowedRoles,
        Func<string, string, int, Task<string>> executor)
    {
        _tools[name] = new RegisteredTool
        {
            Name = name,
            Schema = schema,
            AllowedAgents = allowedAgents,
            AllowedRoles = allowedRoles,
            Executor = executor
        };
    }

    /// <summary>
    /// Get all tool schemas that a specific agent is authorized to use.
    /// </summary>
    public List<GroqToolDto> GetToolsForAgent(string agentName)
    {
        return _tools.Values
            .Where(t => t.AllowedAgents.Contains(agentName) || t.AllowedAgents.Contains("*"))
            .Select(t => t.Schema)
            .ToList();
    }

    /// <summary>
    /// Get all tool schemas available for a given user role.
    /// </summary>
    public List<GroqToolDto> GetToolsForRole(string userRole)
    {
        return _tools.Values
            .Where(t => t.AllowedRoles.Contains(userRole) || t.AllowedRoles.Contains("*"))
            .Select(t => t.Schema)
            .ToList();
    }

    /// <summary>
    /// Execute a tool by name with permission checking.
    /// </summary>
    public async Task<string> ExecuteToolAsync(
        string toolName,
        string argumentsJson,
        string userId,
        int conversationId,
        string agentName,
        string userRole)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            _logger.LogWarning("Unknown tool requested: {ToolName} by agent {AgentName}", toolName, agentName);
            return $"Error: Unknown tool '{toolName}'.";
        }

        if (!tool.AllowedAgents.Contains(agentName) && !tool.AllowedAgents.Contains("*"))
        {
            _logger.LogWarning("Agent {AgentName} is not authorized to use tool {ToolName}", agentName, toolName);
            return $"Error: Agent '{agentName}' is not authorized to use tool '{toolName}'.";
        }

        if (!tool.AllowedRoles.Contains(userRole) && !tool.AllowedRoles.Contains("*"))
        {
            _logger.LogWarning("Role {UserRole} is not authorized to use tool {ToolName}", userRole, toolName);
            return $"Error: Role '{userRole}' is not authorized to use tool '{toolName}'.";
        }

        try
        {
            return await tool.Executor(argumentsJson, userId, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName} for user {UserId}", toolName, userId);
            return $"Error executing {toolName}: {ex.Message}";
        }
    }

    /// <summary>Check if a tool is registered.</summary>
    public bool HasTool(string toolName) => _tools.ContainsKey(toolName);
}
