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
}
