using ContextTax.Core.Transcript;

namespace ContextTax.Core.Counting;

/// <summary>Maps the domain count input (tools + messages) to the count_tokens wire request.</summary>
internal static class CountTokensRequestMapper
{
    public static CountTokensRequest Map(string model, CountInput input)
    {
        var messages = MapMessages(input.Messages);
        var wireTools = input.Tools is { Count: > 0 }
            ? input.Tools.Select(static t => new CountTokensTool(t.Name, t.Description ?? string.Empty, t.InputSchema)).ToArray()
            : null;
        return new CountTokensRequest(model, messages, wireTools);
    }

    private static CountTokensMessage[] MapMessages(IReadOnlyList<TranscriptMessage>? messages)
    {
        // count_tokens requires non-empty messages: a minimal text block is the baseline.
        if (messages is null || messages.Count == 0)
            return [new CountTokensMessage("user", [new CountTokensTextBlock(".")])];

        return messages
            .Select(static m => new CountTokensMessage(m.Role, m.Content.Select(MapBlock).ToArray()))
            .ToArray();
    }

    private static CountTokensContentBlock MapBlock(ContentBlock block) => block switch
    {
        TextBlock t => new CountTokensTextBlock(t.Text),
        ToolUseBlock u => new CountTokensToolUseBlock(u.Id, u.Name, u.Input),
        ToolResultBlock r => new CountTokensToolResultBlock(r.ToolUseId, r.Content),
        _ => throw new InvalidOperationException($"Unknown content block: {block.GetType().Name}"),
    };
}
