using System.Text.Json.Serialization;

namespace OnlineStore.Api.Contracts;

public class GroqChatRequestDto
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "llama-3.3-70b-versatile";

    [JsonPropertyName("messages")]
    public List<GroqMessageDto> Messages { get; set; } = new();

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<GroqToolDto>? Tools { get; set; }

    [JsonPropertyName("stream")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Stream { get; set; }
}

public class GroqToolDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public GroqFunctionDto Function { get; set; } = new();
}

public class GroqFunctionDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public object Parameters { get; set; } = new();
}

public class GroqMessageDto
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<GroqToolCallDto>? ToolCalls { get; set; }

    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }
}

public class GroqChatResponseDto
{
    [JsonPropertyName("choices")]
    public List<GroqChoiceDto> Choices { get; set; } = new();
}

public class GroqChoiceDto
{
    [JsonPropertyName("message")]
    public GroqMessageDto Message { get; set; } = new();
    
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = string.Empty;
}

public class GroqToolCallDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public GroqToolCallFunctionDto Function { get; set; } = new();
}

public class GroqToolCallFunctionDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}

public class GroqStreamChunkDto
{
    [JsonPropertyName("choices")]
    public List<GroqStreamChoiceDto> Choices { get; set; } = new();
}

public class GroqStreamChoiceDto
{
    [JsonPropertyName("delta")]
    public GroqStreamDeltaDto Delta { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public class GroqStreamDeltaDto
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<GroqStreamToolCallDeltaDto>? ToolCalls { get; set; }
}

public class GroqStreamToolCallDeltaDto
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("function")]
    public GroqStreamToolCallFunctionDeltaDto? Function { get; set; }
}

public class GroqStreamToolCallFunctionDeltaDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }
}
