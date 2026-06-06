using System.Text.Json;
using ModelContextProtocol.Client;

namespace ContextTax.Core.Mcp;

/// <summary>An <see cref="IToolSource"/> that connects to a live MCP server via the official SDK,
/// performs the initialize handshake, and returns its tools/list. The only network/process code.
/// Calls only initialize + tools/list — never tools/call.</summary>
public sealed class LiveToolSource : IToolSource
{
    private readonly McpServerConfig _config;
    private readonly TimeSpan _timeout;

    public LiveToolSource(McpServerConfig config, TimeSpan timeout)
    {
        _config = config;
        _timeout = timeout;
    }

    public async Task<IReadOnlyList<McpTool>> GetToolsAsync(CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);

        IClientTransport transport;
        if (_config.Transport == McpTransport.Stdio)
        {
            transport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = _config.Name,
                Command = _config.Command ?? throw new McpConfigException($"MCP server '{_config.Name}' has no command."),
                Arguments = _config.Args?.ToList() ?? [],
                EnvironmentVariables = _config.Env?.ToDictionary(kv => kv.Key, kv => (string?)kv.Value),
            });
        }
        else
        {
            transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Name = _config.Name,
                Endpoint = new Uri(_config.Url ?? throw new McpConfigException($"MCP server '{_config.Name}' has no url.")),
                TransportMode = HttpTransportMode.StreamableHttp,
                AdditionalHeaders = _config.Headers?.ToDictionary(kv => kv.Key, kv => kv.Value),
            });
        }

        McpClient client;
        try
        {
            client = await McpClient.CreateAsync(transport, cancellationToken: cts.Token).ConfigureAwait(false);
        }
        catch
        {
            if (transport is IAsyncDisposable asyncTransport)
                await asyncTransport.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        await using (client.ConfigureAwait(false))
        {
            var tools = await client.ListToolsAsync(cancellationToken: cts.Token).ConfigureAwait(false);
            return tools
                .Select(t => McpToolMapper.Map(t.Name, t.Description, JsonSerializer.SerializeToNode(t.JsonSchema)))
                .ToArray();
        }
    }
}
