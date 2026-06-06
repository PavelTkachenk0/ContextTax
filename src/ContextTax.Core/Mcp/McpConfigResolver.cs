using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ContextTax.Core.Mcp;

/// <summary>One config layer: a friendly source label + that file's <c>mcpServers</c> object.</summary>
public sealed record ConfigLayer(string Source, JsonObject McpServers);

/// <summary>Resolves MCP server definitions from layered <c>mcpServers</c> maps. Earlier layers
/// win on a name collision. Pure — no network. <c>${ENV}</c> placeholders resolve via the
/// injected lookup so it stays testable and secrets stay in the environment. Malformed entries
/// (non-string fields) raise a typed <see cref="McpConfigException"/>, mirroring ToolsJsonLoader.</summary>
public sealed partial class McpConfigResolver
{
    private readonly IReadOnlyList<ConfigLayer> _layers;
    private readonly Func<string, string?> _env;

    public McpConfigResolver(IReadOnlyList<ConfigLayer> layers, Func<string, string?> env)
    {
        _layers = layers;
        _env = env;
    }

    public McpServerConfig Resolve(string name)
    {
        foreach (var layer in _layers)
            if (layer.McpServers[name] is JsonObject entry)
                return Map(name, entry, layer.Source);
        throw new McpConfigException($"No MCP server named '{name}'. Run 'contexttax servers' to list configured servers.");
    }

    public IReadOnlyList<McpServerConfig> List()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<McpServerConfig>();
        foreach (var layer in _layers)
            foreach (var (name, node) in layer.McpServers)
                if (node is JsonObject entry && seen.Add(name))
                    result.Add(Map(name, entry, layer.Source));
        return result;
    }

    private McpServerConfig Map(string name, JsonObject e, string source)
    {
        var type = RequireString(e, "type", name);
        var url = Env(RequireString(e, "url", name), name);
        var command = Env(RequireString(e, "command", name), name);

        var transport = (type, url) switch
        {
            ("http" or "sse", _) => McpTransport.Http,
            ("stdio", _) => McpTransport.Stdio,
            (null, not null) => McpTransport.Http,
            _ => McpTransport.Stdio,
        };

        return new McpServerConfig
        {
            Name = name,
            Transport = transport,
            Source = source,
            Url = url,
            Headers = MapStringMap(e["headers"] as JsonObject, name),
            Command = command,
            Args = MapArgs(e["args"] as JsonArray, name),
            Env = MapStringMap(e["env"] as JsonObject, name),
        };
    }

    /// <summary>String value of <paramref name="key"/>; null if absent; throws if present but not a JSON string.</summary>
    private static string? RequireString(JsonObject e, string key, string server)
    {
        var node = e[key];
        if (node is null)
            return null;
        if (node is not JsonValue v || v.GetValueKind() != JsonValueKind.String)
            throw new McpConfigException($"MCP server '{server}': '{key}' must be a string.");
        return v.GetValue<string>();
    }

    private List<string>? MapArgs(JsonArray? arr, string server)
    {
        if (arr is null)
            return null;
        var list = new List<string>(arr.Count);
        for (var i = 0; i < arr.Count; i++)
        {
            if (arr[i] is not JsonValue v || v.GetValueKind() != JsonValueKind.String)
                throw new McpConfigException($"MCP server '{server}': 'args[{i}]' must be a string.");
            list.Add(Env(v.GetValue<string>(), server)!);
        }
        return list;
    }

    private Dictionary<string, string>? MapStringMap(JsonObject? obj, string server)
    {
        if (obj is null)
            return null;
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (k, v) in obj)
        {
            if (v is not JsonValue val || val.GetValueKind() != JsonValueKind.String)
                throw new McpConfigException($"MCP server '{server}': value for '{k}' must be a string.");
            map[k] = Env(val.GetValue<string>(), server)!;
        }
        return map;
    }

    private string? Env(string? value, string server)
    {
        if (value is null)
            return null;
        return EnvPlaceholder().Replace(value, m =>
        {
            var name = m.Groups[1].Value;
            return _env(name) ?? throw new McpConfigException(
                $"MCP server '{server}': environment variable '{name}' is not set.");
        });
    }

    [GeneratedRegex(@"\$\{([A-Za-z_][A-Za-z0-9_]*)\}")]
    private static partial Regex EnvPlaceholder();
}
