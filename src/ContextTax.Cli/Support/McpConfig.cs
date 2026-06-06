using System.Text.Json;
using System.Text.Json.Nodes;
using ContextTax.Core.Mcp;

namespace ContextTax.Cli.Support;

/// <summary>Builds an <see cref="McpConfigResolver"/> from the real config files: project
/// <c>./.mcp.json</c>, then <c>~/.claude.json</c> (per-project[cwd] then global). A
/// <c>--config</c> path overrides the list with that single file's <c>mcpServers</c>; an
/// explicitly-requested file that is missing or invalid raises a clear error (implicit files
/// are silently skipped).</summary>
public static class McpConfig
{
    public static McpConfigResolver Resolver(string? overridePath = null)
    {
        var layers = new List<ConfigLayer>();
        var cwd = Directory.GetCurrentDirectory();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            AddLayer(layers, overridePath, ParseFile(overridePath, required: true));
        }
        else
        {
            AddLayer(layers, "./.mcp.json", ParseFile(Path.Combine(cwd, ".mcp.json"), required: false));
            var claude = ParseFile(Path.Combine(home, ".claude.json"), required: false);
            AddLayer(layers, "~/.claude.json (project)", claude?["projects"]?[cwd] as JsonObject);
            AddLayer(layers, "~/.claude.json (global)", claude);
        }

        return new McpConfigResolver(layers, name => Environment.GetEnvironmentVariable(name));
    }

    private static JsonObject? ParseFile(string path, bool required)
    {
        if (!File.Exists(path))
        {
            if (required)
                throw new McpConfigException($"config file not found: {path}");
            return null;
        }

        try
        {
            return JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
        }
        catch (JsonException ex)
        {
            if (required)
                throw new McpConfigException($"config file is not valid JSON: {path}", ex);
            return null;
        }
    }

    private static void AddLayer(List<ConfigLayer> layers, string label, JsonObject? section)
    {
        if (section?["mcpServers"] is JsonObject servers)
            layers.Add(new ConfigLayer(label, servers));
    }
}
