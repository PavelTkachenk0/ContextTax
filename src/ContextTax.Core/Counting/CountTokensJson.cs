using System.Text.Json;
using System.Text.Json.Serialization;

namespace ContextTax.Core.Counting;

/// <summary>Shared JSON options for the Anthropic count_tokens wire format.</summary>
internal static class CountTokensJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
