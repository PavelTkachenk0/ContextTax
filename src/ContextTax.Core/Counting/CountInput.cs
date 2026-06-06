using ContextTax.Core.Mcp;
using ContextTax.Core.Transcript;

namespace ContextTax.Core.Counting;

/// <summary>What to count: the tools ("menu") and/or a message history. A typed request
/// snapshot so the seam stays one clean parameter as it grows (e.g. a system prompt later).</summary>
public sealed record CountInput
{
    public IReadOnlyList<McpTool>? Tools { get; init; }
    public IReadOnlyList<TranscriptMessage>? Messages { get; init; }

    /// <summary>A bare baseline (no tools, no history).</summary>
    public static readonly CountInput Empty = new();

    /// <summary>Just the tools (the SP2 schema snapshot).</summary>
    public static CountInput ForTools(IReadOnlyList<McpTool> tools) => new() { Tools = tools };
}
