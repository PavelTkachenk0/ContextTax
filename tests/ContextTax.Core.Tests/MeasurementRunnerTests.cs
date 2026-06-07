using System.Text.Json.Nodes;
using ContextTax.Cli.Support;
using ContextTax.Core.Counting;
using ContextTax.Core.Mcp;
using ContextTax.Core.Measurement;
using Xunit;

namespace ContextTax.Core.Tests;

public class MeasurementRunnerTests
{
    private static McpTool Tool(string n) => new(n, null, new JsonObject());

    private static MeasurementRunner RunnerWith(IToolSource source) =>
        new(new ToolSourceResolver(_ => source));

    private sealed class ThrowingToolSource : IToolSource
    {
        public Task<IReadOnlyList<McpTool>> GetToolsAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("connect boom");
    }

    private sealed class ThrowingTokenCounter : ITokenCounter
    {
        public MeasurementMode Mode => MeasurementMode.GroundTruth;
        public string Label => "throws";
        public Task<int> CountAsync(string model, CountInput input, CancellationToken cancellationToken = default)
            => throw new HttpRequestException("net boom");
    }

    [Fact]
    public async Task RunSchema_returns_report_on_success()
    {
        var tools = new[] { Tool("read_file"), Tool("write_file") };
        var runner = RunnerWith(new FakeToolSource(tools));
        var source = new ToolSourceOptions { Url = "https://h/mcp" };

        var result = await runner.RunSchemaAsync(source, new MeasurementOptions { Model = "test-model" }, new FakeTokenCounter());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Report!.ToolCount);
    }

    [Fact]
    public async Task RunSchema_no_source_fails_with_exit_2()
    {
        var runner = RunnerWith(new FakeToolSource(Array.Empty<McpTool>()));

        var result = await runner.RunSchemaAsync(new ToolSourceOptions(), new MeasurementOptions { Model = "test-model" }, new FakeTokenCounter());

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.ExitCode);
        Assert.Contains("tool source", result.ErrorMessage);
    }

    [Fact]
    public async Task RunSchema_source_failure_fails_with_exit_1()
    {
        var runner = RunnerWith(new ThrowingToolSource());
        var source = new ToolSourceOptions { Url = "https://h/mcp" };

        var result = await runner.RunSchemaAsync(source, new MeasurementOptions { Model = "test-model" }, new FakeTokenCounter());

        Assert.False(result.IsSuccess);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("failed to read tools", result.ErrorMessage);
    }

    [Fact]
    public async Task RunSchema_measure_failure_fails_with_exit_1()
    {
        var tools = new[] { Tool("read_file") };
        var runner = RunnerWith(new FakeToolSource(tools));
        var source = new ToolSourceOptions { Url = "https://h/mcp" };

        var result = await runner.RunSchemaAsync(source, new MeasurementOptions { Model = "test-model" }, new ThrowingTokenCounter());

        Assert.False(result.IsSuccess);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("network failure", result.ErrorMessage);
    }

    [Fact]
    public async Task RunSession_missing_transcript_fails_with_exit_2()
    {
        var runner = RunnerWith(new FakeToolSource(Array.Empty<McpTool>()));

        var result = await runner.RunSessionAsync(
            "/no/such/transcript.json", source: null, new MeasurementOptions { Model = "test-model" }, new FakeTokenCounter());

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.ExitCode);
        Assert.Contains("file not found", result.ErrorMessage);
    }

    [Fact]
    public async Task RunSession_returns_report_on_success()
    {
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, """
        { "messages": [
          { "role": "assistant", "content": [ { "type": "tool_use", "id": "t1", "name": "read_file", "input": { "path": "/x" } } ] },
          { "role": "user", "content": [ { "type": "tool_result", "tool_use_id": "t1", "content": "ok" } ] }
        ] }
        """);

        var runner = RunnerWith(new FakeToolSource(Array.Empty<McpTool>()));
        var result = await runner.RunSessionAsync(path, source: null, new MeasurementOptions { Model = "test-model" }, new FakeTokenCounter());

        File.Delete(path);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Report);
    }

    [Fact]
    public async Task RunResponse_returns_report_on_success()
    {
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, "captured response");
        try
        {
            var runner = RunnerWith(new FakeToolSource(Array.Empty<McpTool>()));
            var result = await runner.RunResponseAsync(path, new MeasurementOptions { Model = "m1" }, new FakeTokenCounter());

            Assert.True(result.IsSuccess);
            Assert.True(result.Report!.ResponseTokens > 0);
            Assert.Equal(Path.GetFileName(path), result.Report!.Source);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task RunResponse_reads_stdin_for_dash()
    {
        var originalIn = Console.In;
        Console.SetIn(new StringReader("a captured response"));
        try
        {
            var runner = RunnerWith(new FakeToolSource(Array.Empty<McpTool>()));
            var result = await runner.RunResponseAsync("-", new MeasurementOptions { Model = "m1" }, new FakeTokenCounter());

            Assert.True(result.IsSuccess);
            Assert.Equal("(stdin)", result.Report!.Source);
        }
        finally { Console.SetIn(originalIn); }
    }

    [Fact]
    public async Task RunResponse_missing_file_fails_with_exit_2()
    {
        var runner = RunnerWith(new FakeToolSource(Array.Empty<McpTool>()));
        var result = await runner.RunResponseAsync("/no/such/resp.json", new MeasurementOptions { Model = "m1" }, new FakeTokenCounter());

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.ExitCode);
        Assert.Contains("file not found", result.ErrorMessage);
    }

    [Fact]
    public async Task RunResponse_empty_file_fails_with_exit_2()
    {
        var path = Path.GetTempFileName();   // 0 bytes
        try
        {
            var runner = RunnerWith(new FakeToolSource(Array.Empty<McpTool>()));
            var result = await runner.RunResponseAsync(path, new MeasurementOptions { Model = "m1" }, new FakeTokenCounter());

            Assert.False(result.IsSuccess);
            Assert.Equal(2, result.ExitCode);
            Assert.Contains("empty", result.ErrorMessage);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task RunResponseDelta_returns_diff_on_success()
    {
        var before = Path.GetTempFileName();
        var after = Path.GetTempFileName();
        await File.WriteAllTextAsync(before, "a much longer response payload than the other");
        await File.WriteAllTextAsync(after, "short");
        try
        {
            var runner = RunnerWith(new FakeToolSource(Array.Empty<McpTool>()));
            var result = await runner.RunResponseDeltaAsync(before, after, new MeasurementOptions { Model = "m1" }, new FakeTokenCounter());

            Assert.True(result.IsSuccess);
            Assert.True(result.Report!.DeltaTokens < 0);   // after is shorter
        }
        finally { File.Delete(before); File.Delete(after); }
    }

    [Fact]
    public async Task RunResponseDelta_both_stdin_fails_with_exit_2()
    {
        var runner = RunnerWith(new FakeToolSource(Array.Empty<McpTool>()));
        var result = await runner.RunResponseDeltaAsync("-", "-", new MeasurementOptions { Model = "m1" }, new FakeTokenCounter());

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.ExitCode);
        Assert.Contains("stdin", result.ErrorMessage);
    }

    [Fact]
    public async Task RunResponseText_measures_in_memory_text()
    {
        var runner = RunnerWith(new FakeToolSource(Array.Empty<McpTool>()));
        var result = await runner.RunResponseTextAsync("(clipboard)", "pasted response", new MeasurementOptions { Model = "m1" }, new FakeTokenCounter());

        Assert.True(result.IsSuccess);
        Assert.True(result.Report!.ResponseTokens > 0);
        Assert.Equal("(clipboard)", result.Report!.Source);
    }

    [Fact]
    public async Task RunResponseText_empty_fails_with_exit_2()
    {
        var runner = RunnerWith(new FakeToolSource(Array.Empty<McpTool>()));
        var result = await runner.RunResponseTextAsync("(clipboard)", "   ", new MeasurementOptions { Model = "m1" }, new FakeTokenCounter());

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.ExitCode);
        Assert.Contains("empty", result.ErrorMessage);
    }

    [Fact]
    public async Task RunResponse_unreadable_path_fails_with_exit_2()
    {
        // A pasted multi-line blob given where a path is expected → not a readable file → exit 2,
        // WITHOUT echoing the blob back in the message.
        var runner = RunnerWith(new FakeToolSource(Array.Empty<McpTool>()));
        var blob = "{\"success\":true,\n\"x\":1}";
        var result = await runner.RunResponseAsync(blob, new MeasurementOptions { Model = "m1" }, new FakeTokenCounter());

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.ExitCode);
        Assert.DoesNotContain("success", result.ErrorMessage);   // the blob is not echoed
    }
}
