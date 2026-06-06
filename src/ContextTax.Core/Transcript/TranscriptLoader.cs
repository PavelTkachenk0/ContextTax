using System.Text.Json;
using System.Text.Json.Nodes;
using ContextTax.Core.Mcp;

namespace ContextTax.Core.Transcript;

/// <summary>Parses an Anthropic-style messages document into a <see cref="SessionTranscript"/>.
/// Accepts a bare messages array or an object with a "messages" array and an optional
/// embedded "tools" array (embedded tools win over the external fallback).</summary>
public static class TranscriptLoader
{
    public static SessionTranscript LoadFile(string path, IReadOnlyList<McpTool>? externalTools = null)
        => Load(File.ReadAllText(path), externalTools);

    public static SessionTranscript Load(string json, IReadOnlyList<McpTool>? externalTools = null)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new TranscriptException($"Invalid JSON: {ex.Message}", ex);
        }

        var (messagesNode, embeddedTools) = root switch
        {
            JsonArray arr => (arr, (JsonArray?)null),
            JsonObject obj when obj["messages"] is JsonArray arr => (arr, obj["tools"] as JsonArray),
            _ => throw new TranscriptException(
                "Expected a JSON array of messages, or an object with a 'messages' array."),
        };

        var tools = embeddedTools is not null
            ? ToolsJsonLoader.LoadArray(embeddedTools)
            : externalTools ?? Array.Empty<McpTool>();

        var messages = new List<TranscriptMessage>(messagesNode.Count);
        foreach (var node in messagesNode)
            messages.Add(ParseMessage(node));

        return new SessionTranscript(tools, messages);
    }

    private static TranscriptMessage ParseMessage(JsonNode? node)
    {
        if (node is not JsonObject obj)
            throw new TranscriptException("Each message must be a JSON object.");

        if (obj["role"] is not JsonValue roleVal || roleVal.GetValueKind() != JsonValueKind.String)
            throw new TranscriptException("Each message must have a string 'role'.");
        var role = roleVal.GetValue<string>();

        IReadOnlyList<ContentBlock> blocks = obj["content"] switch
        {
            JsonValue v when v.GetValueKind() == JsonValueKind.String
                => [new TextBlock(v.GetValue<string>())],
            JsonArray arr => ParseBlocks(arr),
            _ => throw new TranscriptException(
                $"Message 'content' must be a string or an array of blocks (role '{role}')."),
        };

        return new TranscriptMessage(role, blocks);
    }

    private static List<ContentBlock> ParseBlocks(JsonArray arr)
    {
        var blocks = new List<ContentBlock>(arr.Count);
        foreach (var node in arr)
        {
            if (node is not JsonObject b
                || b["type"] is not JsonValue t
                || t.GetValueKind() != JsonValueKind.String)
            {
                throw new TranscriptException("Each content block must be an object with a string 'type'.");
            }

            var type = t.GetValue<string>();
            blocks.Add(type switch
            {
                "text" => new TextBlock(b["text"]?.GetValue<string>() ?? string.Empty),
                "tool_use" => new ToolUseBlock(
                    RequiredString(b, "id", "tool_use"),
                    RequiredString(b, "name", "tool_use"),
                    b["input"]?.DeepClone() ?? new JsonObject()),
                "tool_result" => new ToolResultBlock(
                    RequiredString(b, "tool_use_id", "tool_result"),
                    b["content"]?.DeepClone() ?? JsonValue.Create(string.Empty)!),
                _ => throw new TranscriptException($"Unknown content block type '{type}'."),
            });
        }

        return blocks;
    }

    private static string RequiredString(JsonObject block, string field, string blockType)
    {
        if (block[field] is JsonValue v && v.GetValueKind() == JsonValueKind.String)
            return v.GetValue<string>();
        throw new TranscriptException($"A '{blockType}' block requires a string '{field}'.");
    }
}
