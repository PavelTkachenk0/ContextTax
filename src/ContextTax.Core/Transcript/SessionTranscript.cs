using ContextTax.Core.Mcp;

namespace ContextTax.Core.Transcript;

/// <summary>A loaded transcript: the tools ("menu") plus the message timeline.</summary>
public sealed record SessionTranscript(
    IReadOnlyList<McpTool> Tools,
    IReadOnlyList<TranscriptMessage> Messages);
