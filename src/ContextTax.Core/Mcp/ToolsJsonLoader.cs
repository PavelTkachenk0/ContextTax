using System.Text.Json;
using System.Text.Json.Nodes;

namespace ContextTax.Core.Mcp;

/// <summary>Parses a tools-JSON document (MCP tools/list shape, or a bare array).</summary>
public static class ToolsJsonLoader
{
    public static IReadOnlyList<McpTool> LoadFile(string path)
        => Load(File.ReadAllText(path));

    public static IReadOnlyList<McpTool> Load(string json)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new ToolsJsonException($"Invalid JSON: {ex.Message}", ex);
        }

        var array = root switch
        {
            JsonArray arr => arr,
            JsonObject obj when obj["tools"] is JsonArray arr => arr,
            _ => throw new ToolsJsonException(
                "Expected a JSON array of tools, or an object with a 'tools' array."),
        };

        return LoadArray(array);
    }

    internal static IReadOnlyList<McpTool> LoadArray(JsonArray array)
    {
        var tools = new List<McpTool>(array.Count);
        foreach (var node in array)
        {
            if (node is not JsonObject obj)
                throw new ToolsJsonException("Each tool must be a JSON object.");

            if (obj["name"] is not JsonValue nameVal || nameVal.GetValueKind() != JsonValueKind.String)
                throw new ToolsJsonException("Each tool must have a string 'name'.");
            var name = nameVal.GetValue<string>();

            var description =
                obj["description"] is JsonValue d && d.GetValueKind() == JsonValueKind.String
                    ? d.GetValue<string>()
                    : null;

            var schemaNode = obj["inputSchema"] ?? obj["input_schema"];
            var inputSchema = schemaNode?.DeepClone() ?? new JsonObject();

            tools.Add(new McpTool(name, description, inputSchema));
        }

        return tools;
    }
}
