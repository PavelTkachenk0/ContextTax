using System.Text.Json.Nodes;

namespace ContextTax.Core.Counting;

/// <summary>Typed request body for POST /v1/messages/count_tokens (wire model — data only).</summary>
internal sealed record CountTokensRequest(
    string Model,
    IReadOnlyList<CountTokensMessage> Messages,
    IReadOnlyList<CountTokensTool>? Tools);

internal sealed record CountTokensMessage(string Role, string Content);

internal sealed record CountTokensTool(string Name, string Description, JsonNode InputSchema);
