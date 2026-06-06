namespace ContextTax.Core.Transcript;

/// <summary>One message in a recorded agent transcript (domain model).</summary>
public sealed record TranscriptMessage(string Role, IReadOnlyList<ContentBlock> Content);
