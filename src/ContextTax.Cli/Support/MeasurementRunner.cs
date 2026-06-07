using ContextTax.Core.Counting;
using ContextTax.Core.Mcp;
using ContextTax.Core.Measurement;
using ContextTax.Core.Transcript;

namespace ContextTax.Cli.Support;

/// <summary>The outcome of a measurement run: either a report, or a friendly error + exit code.</summary>
public sealed record RunResult<T>(T? Report, string? ErrorMessage, int ExitCode) where T : class
{
    // Success ⟺ Report is non-null — guaranteed by the Ok/Fail factories (the only intended constructors).
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

#pragma warning disable CA1822 // Response methods are instance for API consistency with RunSchemaAsync/RunSessionAsync.
    public async Task<RunResult<ResponseCostReport>> RunResponseAsync(
        string pathOrDash, MeasurementOptions options, ITokenCounter counter, CancellationToken ct = default)
    {
        var (text, error) = ReadInput(pathOrDash);
        if (text is null)
            return RunResult.Fail<ResponseCostReport>(error!, 2);

        return await MeasureAsync(() =>
            new ResponseCostMeasurer(counter).MeasureAsync(SourceLabel(pathOrDash), text, options, ct)).ConfigureAwait(false);
    }

    public async Task<RunResult<ResponseDiffReport>> RunResponseDeltaAsync(
        string beforePath, string afterPath, MeasurementOptions options, ITokenCounter counter, CancellationToken ct = default)
    {
        if (beforePath == "-" && afterPath == "-")
            return RunResult.Fail<ResponseDiffReport>("stdin (-) can only be used for one input.", 2);

        var (beforeText, beforeError) = ReadInput(beforePath);
        if (beforeText is null)
            return RunResult.Fail<ResponseDiffReport>(beforeError!, 2);

        var (afterText, afterError) = ReadInput(afterPath);
        if (afterText is null)
            return RunResult.Fail<ResponseDiffReport>(afterError!, 2);

        return await MeasureAsync(async () =>
        {
            var measurer = new ResponseCostMeasurer(counter);
            var before = await measurer.MeasureAsync(SourceLabel(beforePath), beforeText, options, ct).ConfigureAwait(false);
            var after = await measurer.MeasureAsync(SourceLabel(afterPath), afterText, options, ct).ConfigureAwait(false);
            return ResponseDiff.Between(before, after);
        }).ConfigureAwait(false);
    }

    public async Task<RunResult<ResponseCostReport>> RunResponseTextAsync(
        string source, string responseText, MeasurementOptions options, ITokenCounter counter, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return RunResult.Fail<ResponseCostReport>("response is empty.", 2);

        return await MeasureAsync(() =>
            new ResponseCostMeasurer(counter).MeasureAsync(source, responseText, options, ct)).ConfigureAwait(false);
    }

#pragma warning restore CA1822

    // Reads a response payload from a file or, for "-", stdin. Returns (null, message) on a
    // not-found / empty input so callers can map it to a friendly exit-2.
    private static (string? Text, string? Error) ReadInput(string pathOrDash)
    {
        string text;
        if (pathOrDash == "-")
        {
            text = Console.In.ReadToEnd();
        }
        else
        {
            // Reject strings that are clearly not file paths (contain newlines, excessively long,
            // or contain characters that cannot appear in any path). This guards against a pasted
            // blob being echoed back in the error message.
            if (pathOrDash.Contains('\n') || pathOrDash.Contains('\r') || pathOrDash.Length > 4096)
                return (null, "could not read that as a file — to measure pasted text use the clipboard option, or pipe it (pbpaste | contexttax response).");

            try
            {
                text = File.ReadAllText(pathOrDash);
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            {
                return (null, $"file not found: {pathOrDash}");
            }
            catch (Exception e) when (e is PathTooLongException or ArgumentException or IOException or UnauthorizedAccessException)
            {
                return (null, "could not read that as a file — to measure pasted text use the clipboard option, or pipe it (pbpaste | contexttax response).");
            }
        }

        return string.IsNullOrWhiteSpace(text) ? (null, "response is empty.") : (text, null);
    }

    private static string SourceLabel(string pathOrDash) =>
        pathOrDash == "-" ? "(stdin)" : Path.GetFileName(pathOrDash);

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
