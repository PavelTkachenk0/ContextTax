using System.Text.Json.Nodes;
using ContextTax.Core.Transcript;

namespace ContextTax.Core.Measurement;

/// <summary>
/// Anthropic <c>count_tokens</c> rejects (HTTP 400) any <c>tool_use</c> that is not immediately
/// followed by a matching <c>tool_result</c>. The marginal-delta measurers build message prefixes
/// that can end at a <c>tool_use</c>; this pads such a sequence with a synthetic <b>empty</b>
/// <c>tool_result</c> so the request is valid. The empty content is constant, so it cancels in any
/// marginal delta (it shifts the constant block framing, never the measured payload). See ADR 0012.
/// </summary>
internal static class ToolResultPadding
{
    /// <summary>True if the last message carries a <c>tool_use</c> with no matching <c>tool_result</c>.</summary>
    public static bool EndsWithDanglingToolUse(IReadOnlyList<TranscriptMessage> messages) =>
        messages.Count > 0 && UnmatchedToolUseIds(messages[^1]).Count > 0;

    /// <summary>
    /// Returns <paramref name="messages"/> unchanged, or — if it ends at an unmatched <c>tool_use</c> —
    /// with a trailing <c>user</c> message of empty <c>tool_result</c>(s) so it is valid for count_tokens.
    /// </summary>
    public static TranscriptMessage[] PadDanglingToolUse(IReadOnlyList<TranscriptMessage> messages)
    {
        if (messages.Count == 0)
            return Array.Empty<TranscriptMessage>();

        var unmatched = UnmatchedToolUseIds(messages[^1]);
        if (unmatched.Count == 0)
            return messages as TranscriptMessage[] ?? messages.ToArray();

        var pad = new TranscriptMessage("user",
            unmatched.Select(id => (ContentBlock)new ToolResultBlock(id, JsonValue.Create(string.Empty)!)).ToArray());

        var padded = new TranscriptMessage[messages.Count + 1];
        for (var i = 0; i < messages.Count; i++)
            padded[i] = messages[i];
        padded[messages.Count] = pad;
        return padded;
    }

    private static List<string> UnmatchedToolUseIds(TranscriptMessage message)
    {
        var resultIds = message.Content.OfType<ToolResultBlock>().Select(b => b.ToolUseId).ToHashSet();
        return message.Content.OfType<ToolUseBlock>().Select(b => b.Id).Where(id => !resultIds.Contains(id)).ToList();
    }
}
