using ContextTax.Core.Counting;
using ContextTax.Core.Mcp;
using ContextTax.Core.Measurement;
using ContextTax.Core.Transcript;

namespace ContextTax.Cli.Support;

/// <summary>The outcome of a measurement run: either a report, or a friendly error + exit code.</summary>
public sealed record RunResult<T>(T? Report, string? ErrorMessage, int ExitCode) where T : class
{
    public bool IsSuccess => ErrorMessage is null;
}

/// <summary>Non-generic factory for <see cref="RunResult{T}"/> (avoids CA1000).</summary>
public static class RunResult
{
    public static RunResult<T> Ok<T>(T report) where T : class => new(report, null, 0);
    public static RunResult<T> Fail<T>(string message, int exitCode) where T : class => new(null, message, exitCode);
}

/// <summary>Shared resolve→count→measure core used by both the flag commands and the interactive
/// mode. Maps every failure to a friendly (message, exitCode) so callers just render the result.
/// The counter is supplied by the caller (which owns its lifetime) and is already validated, so a
/// missing key never reaches resolution.</summary>
public sealed class MeasurementRunner
{
    private readonly ToolSourceResolver _resolver;

    public MeasurementRunner(ToolSourceResolver resolver) => _resolver = resolver;

    public static MeasurementRunner Default(TimeSpan timeout) => new(ToolSourceResolver.Default(timeout));

    public async Task<RunResult<SchemaCostReport>> RunSchemaAsync(
        ToolSourceOptions source, MeasurementOptions options, ITokenCounter counter, CancellationToken ct = default)
    {
        IReadOnlyList<McpTool> tools;
        try
        {
            tools = await _resolver.ResolveAsync(source, ct).ConfigureAwait(false);
        }
        catch (ToolSourceException ex)
        {
            return RunResult.Fail<SchemaCostReport>(ex.Message, ex.ExitCode);
        }

        return await MeasureAsync(() => new SchemaCostMeasurer(counter).MeasureAsync(tools, options)).ConfigureAwait(false);
    }

    public async Task<RunResult<SessionCostReport>> RunSessionAsync(
        string transcriptPath, ToolSourceOptions? source, MeasurementOptions options, ITokenCounter counter, CancellationToken ct = default)
    {
        IReadOnlyList<McpTool>? externalTools = null;
        if (source is not null && HasSource(source))
        {
            try
            {
                externalTools = await _resolver.ResolveAsync(source, ct).ConfigureAwait(false);
            }
            catch (ToolSourceException ex)
            {
                return RunResult.Fail<SessionCostReport>(ex.Message, ex.ExitCode);
            }
        }

        SessionTranscript transcript;
        try
        {
            transcript = TranscriptLoader.LoadFile(transcriptPath, externalTools);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return RunResult.Fail<SessionCostReport>($"file not found: {transcriptPath}", 2);
        }
        catch (TranscriptException ex)
        {
            return RunResult.Fail<SessionCostReport>(ex.Message, 2);
        }

        return await MeasureAsync(() => new SessionCostMeasurer(counter).MeasureAsync(transcript, options)).ConfigureAwait(false);
    }

    private static async Task<RunResult<T>> MeasureAsync<T>(Func<Task<T>> measure) where T : class
    {
        try
        {
            return RunResult.Ok(await measure().ConfigureAwait(false));
        }
        catch (TokenCountException ex)
        {
            return RunResult.Fail<T>(ex.Message, 1);
        }
        catch (HttpRequestException ex)
        {
            return RunResult.Fail<T>($"network failure calling Anthropic: {ex.Message}", 1);
        }
    }

    private static bool HasSource(ToolSourceOptions s) =>
        !string.IsNullOrWhiteSpace(s.ToolsPath) || !string.IsNullOrWhiteSpace(s.ServerName) || !string.IsNullOrWhiteSpace(s.Url);
}
