using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ContextTax.Core.Counting;

/// <summary>Typed request body for POST /v1/messages/count_tokens (wire model — data only).</summary>
internal sealed record CountTokensRequest(
    string Model,
    IReadOnlyList<CountTokensMessage> Messages,
    IReadOnlyList<CountTokensTool>? Tools);

internal sealed record CountTokensMessage(string Role, IReadOnlyList<CountTokensContentBlock> Content);

internal sealed record CountTokensTool(string Name, string Description, JsonNode InputSchema);

/// <summary>Wire content block. The "type" discriminator is emitted by the polymorphic
/// serializer; other property names follow the shared snake_case policy.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CountTokensTextBlock), "text")]
[JsonDerivedType(typeof(CountTokensToolUseBlock), "tool_use")]
[JsonDerivedType(typeof(CountTokensToolResultBlock), "tool_result")]
internal abstract record CountTokensContentBlock;

internal sealed record CountTokensTextBlock(string Text) : CountTokensContentBlock;

internal sealed record CountTokensToolUseBlock(string Id, string Name, JsonNode Input) : CountTokensContentBlock;

internal sealed record CountTokensToolResultBlock(string ToolUseId, JsonNode Content) : CountTokensContentBlock;
