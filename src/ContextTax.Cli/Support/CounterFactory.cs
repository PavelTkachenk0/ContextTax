using ContextTax.Core.Counting;

namespace ContextTax.Cli.Support;

/// <summary>Selects the token counter from CLI settings and owns the <see cref="HttpClient"/>
/// lifetime (the ground-truth counter wraps it). On a missing key for the ground-truth path,
/// returns a null counter plus a hint. Dispose to release the client.</summary>
/// <remarks><see cref="Error"/> is a bare message — callers write it with their standard
/// <c>"error: "</c> prefix (as they do for every other CLI error). <see cref="Counter"/> wraps
/// the owned <see cref="HttpClient"/>, so do not use it after <see cref="Dispose"/>.</remarks>
public sealed class CounterFactory : IDisposable
{
    private readonly HttpClient? _http;

    public ITokenCounter? Counter { get; }
    public string? Error { get; }

    private CounterFactory(ITokenCounter? counter, string? error, HttpClient? http)
    {
        Counter = counter;
        Error = error;
        _http = http;
    }

    public static CounterFactory Create(bool estimate, string? apiKey)
    {
        if (estimate)
            return new CounterFactory(EstimateTokenCounter.CreateO200k(), null, null);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new CounterFactory(
                null,
                "ANTHROPIC_API_KEY is not set. Run with --estimate for a keyless approximate count, "
                + "or set the key (or use 'dotnet user-secrets') for exact ground-truth.",
                null);
        }

        var http = new HttpClient();
        return new CounterFactory(new AnthropicTokenCounter(new AnthropicCountTokensClient(http, apiKey)), null, http);
    }

    public void Dispose() => _http?.Dispose();
}
