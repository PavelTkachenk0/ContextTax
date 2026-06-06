namespace ContextTax.Core.Mcp;

public enum McpTransport
{
    Stdio,
    Http,
}

/// <summary>A resolved MCP server definition (one entry of an mcpServers config), with
/// <c>${ENV}</c> placeholders already resolved. Header/env values are secrets — never logged.</summary>
public sealed record McpServerConfig
{
    public required string Name { get; init; }
    public required McpTransport Transport { get; init; }
    public required string Source { get; init; }
    public string? Url { get; init; }
    public IReadOnlyDictionary<string, string>? Headers { get; init; }
    public string? Command { get; init; }
    public IReadOnlyList<string>? Args { get; init; }
    public IReadOnlyDictionary<string, string>? Env { get; init; }
}
