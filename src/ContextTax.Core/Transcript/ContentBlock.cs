using System.Text.Json.Nodes;

namespace ContextTax.Core.Transcript;

/// <summary>One content block inside an agent message (domain model).</summary>
public abstract record ContentBlock;

/// <summary>Plain text content.</summary>
public sealed record TextBlock(string Text) : ContentBlock;

/// <summary>An assistant tool invocation.</summary>
public sealed record ToolUseBlock(string Id, string Name, JsonNode Input) : ContentBlock;

/// <summary>A tool's result returned to the model. <c>Content</c> is raw
/// (a JSON string or array of blocks), kept as-is for faithful token counting.</summary>
public sealed record ToolResultBlock(string ToolUseId, JsonNode Content) : ContentBlock;
