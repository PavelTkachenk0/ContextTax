using ContextTax.Core.Mcp;

namespace ContextTax.Cli.Support;

public sealed class ToolSourceException : Exception
{
    public ToolSourceException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>The tool-source inputs from a command's settings (exactly one of the three).</summary>
public sealed record ToolSourceOptions
{
    public string? ToolsPath { get; init; }
    public string? ServerName { get; init; }
    public string? Url { get; init; }
    public Dictionary<string, string>? Headers { get; init; }
    public string? ConfigPath { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
}

/// <summary>Resolves the chosen tool source (--tools | --server | --url) to <c>McpTool[]</c>.
/// The live-source factory is injected so tests use a fake; production builds a
/// <see cref="LiveToolSource"/>.</summary>
public sealed class ToolSourceResolver
{
    private readonly Func<McpServerConfig, IToolSource> _liveSourceFactory;

    public ToolSourceResolver(Func<McpServerConfig, IToolSource> liveSourceFactory)
        => _liveSourceFactory = liveSourceFactory;

    public static ToolSourceResolver Default(TimeSpan timeout) =>
        new(cfg => new LiveToolSource(cfg, timeout));

    public async Task<IReadOnlyList<McpTool>> ResolveAsync(ToolSourceOptions options, CancellationToken ct = default)
    {
        var chosen = new[] { options.ToolsPath, options.ServerName, options.Url }.Count(s => !string.IsNullOrWhiteSpace(s));
        if (chosen == 0)
            throw new ToolSourceException("a tool source is required: one of --tools <path>, --server <name>, or --url <url>.");
        if (chosen > 1)
            throw new ToolSourceException("--tools, --server and --url are mutually exclusive — choose one.");

        if (!string.IsNullOrWhiteSpace(options.ToolsPath))
        {
            try
            {
                return ToolsJsonLoader.LoadFile(options.ToolsPath);
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            {
                throw new ToolSourceException($"file not found: {options.ToolsPath}");
            }
            catch (ToolsJsonException ex)
            {
                throw new ToolSourceException(ex.Message, ex);
            }
        }

        McpServerConfig config;
        if (!string.IsNullOrWhiteSpace(options.ServerName))
        {
            try
            {
                config = McpConfig.Resolver(options.ConfigPath).Resolve(options.ServerName);
            }
            catch (McpConfigException ex)
            {
                throw new ToolSourceException(ex.Message, ex);
            }
        }
        else
        {
            config = new McpServerConfig
            {
                Name = DisplayName(options.Url!),
                Transport = McpTransport.Http,
                Source = "--url",
                Url = options.Url,
                Headers = options.Headers,
            };
        }

        var source = _liveSourceFactory(config);
        try
        {
            return await source.GetToolsAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not ToolSourceException)
        {
            throw new ToolSourceException($"failed to read tools from '{config.Name}': {ex.Message}", ex);
        }
    }

    private static string DisplayName(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.GetLeftPart(UriPartial.Authority) : url;
}
