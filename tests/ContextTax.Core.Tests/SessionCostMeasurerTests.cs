using System.Text.Json.Nodes;
using ContextTax.Core.Counting;
using ContextTax.Core.Mcp;
using ContextTax.Core.Measurement;
using ContextTax.Core.Transcript;
using Xunit;

namespace ContextTax.Core.Tests;

public class SessionCostMeasurerTests
{
    private static MeasurementOptions Options => new()
    {
        Model = "test-model",
        ContextWindowTokens = 200_000,
    };

    // One canonical turn: assistant tool_use "rf" + user tool_result.
    // FakeTokenCounter weights (Baseline 10):
    //   tools = [ "rf" ]                                            -> +2
    //   tool_use: name "rf" (2) + input {"p":"x"} (9)               -> 11
    //   call = tool_use (11) + empty-result PADDING ("" -> 2)       -> 13  (ADR 0012: the prefix ending
    //          at the tool_use is padded so count_tokens accepts it; the constant framing lands in call)
    //   tool_result "RESULT" -> "\"RESULT\"" (8), minus padded "" (2) -> response = 6
    //   pair total (13 + 6 = 19) and cumulative (31) are unchanged by padding.
    private static SessionTranscript Canonical()
    {
        var toolUse = new TranscriptMessage("assistant", new ContentBlock[]
        {
            new ToolUseBlock("t1", "rf", JsonNode.Parse("""{"p":"x"}""")!),
        });
        var toolResult = new TranscriptMessage("user", new ContentBlock[]
        {
            new ToolResultBlock("t1", JsonNode.Parse("\"RESULT\"")!),
        });
        return new SessionTranscript(
            new[] { new McpTool("rf", null, new JsonObject()) },
            new[] { toolUse, toolResult });
    }

    [Fact]
    public async Task Splits_call_and_response_and_tracks_cumulative()
    {
        var measurer = new SessionCostMeasurer(new FakeTokenCounter());

        var report = await measurer.MeasureAsync(Canonical(), Options);

        Assert.Equal(1, report.TurnCount);
        Assert.Equal(2, report.SchemaTokens);                 // start(12) - empty(10)

        var turn = Assert.Single(report.Turns);
        Assert.Equal(1, turn.Index);
        Assert.Equal("rf", turn.ToolName);
        Assert.Equal(13, turn.CallTokens);
        Assert.Equal(6, turn.ResponseTokens);
        Assert.Equal(19, turn.AddedTokens);                   // 13 + 6
        Assert.Equal(31, turn.CumulativeTokens);              // 10 + 2 + 13 + 6

        Assert.Equal(13, report.CallsTotal);
        Assert.Equal(6, report.ResponsesTotal);
        Assert.Equal(31, report.PeakContextTokens);
        Assert.Equal(31.0 / 200_000 * 100, turn.PercentWindow, 6);
        Assert.Equal(31.0 / 200_000 * 100, report.PeakPercentWindow, 6);
        Assert.Equal(6.0 / 2, report.ResponseToSchemaRatio, 6);       // 3.0
        Assert.Equal(6.0 / 31, report.ResponseShareOfContext, 6);
    }

    [Fact]
    public async Task Forwards_counter_mode_and_label()
    {
        var counter = new FakeTokenCounter(MeasurementMode.Estimate, "o200k_base (offline proxy)");
        var measurer = new SessionCostMeasurer(counter);

        var report = await measurer.MeasureAsync(Canonical(), Options);

        Assert.Equal(MeasurementMode.Estimate, report.Mode);
        Assert.Equal("o200k_base (offline proxy)", report.CounterLabel);
        Assert.Equal("test-model", report.ModelId);
    }

    [Fact]
    public async Task Non_tool_message_becomes_a_dash_row()
    {
        var userText = new TranscriptMessage("user", new ContentBlock[] { new TextBlock("hello") });
        var transcript = new SessionTranscript(Array.Empty<McpTool>(), new[] { userText });
        var measurer = new SessionCostMeasurer(new FakeTokenCounter());

        var report = await measurer.MeasureAsync(transcript, Options);

        var row = Assert.Single(report.Turns);
        Assert.Equal("—", row.ToolName);
        Assert.Equal(0, row.CallTokens);
        Assert.Equal(0, row.ResponseTokens);
        Assert.Equal(5, row.AddedTokens);                     // "hello".Length
        Assert.Equal(0, report.CallsTotal);
        Assert.Equal(0, report.ResponsesTotal);
    }

    [Fact]
    public async Task Orphan_tool_result_without_a_preceding_call_becomes_a_dash_row()
    {
        // A tool_result with no open turn is a documented v1 limitation: its tokens show up
        // in Added/Cumulative (so % window stays correct) but are NOT counted as a response.
        var orphan = new TranscriptMessage("user", new ContentBlock[]
        {
            new ToolResultBlock("x", JsonNode.Parse("\"DATA\"")!),
        });
        var transcript = new SessionTranscript(Array.Empty<McpTool>(), new[] { orphan });
        var measurer = new SessionCostMeasurer(new FakeTokenCounter());

        var report = await measurer.MeasureAsync(transcript, Options);

        var row = Assert.Single(report.Turns);
        Assert.Equal("—", row.ToolName);
        Assert.Equal(0, row.CallTokens);
        Assert.Equal(0, row.ResponseTokens);
        Assert.Equal(6, row.AddedTokens);          // "\"DATA\"".ToJsonString() length
        Assert.Equal(0, report.ResponsesTotal);
        Assert.Equal(0, report.CallsTotal);
    }

    [Fact]
    public async Task Empty_transcript_yields_no_turns()
    {
        var transcript = new SessionTranscript(
            new[] { new McpTool("rf", null, new JsonObject()) }, Array.Empty<TranscriptMessage>());
        var measurer = new SessionCostMeasurer(new FakeTokenCounter());

        var report = await measurer.MeasureAsync(transcript, Options);

        Assert.Equal(0, report.TurnCount);
        Assert.Empty(report.Turns);
        Assert.Equal(2, report.SchemaTokens);
        Assert.Equal(12, report.PeakContextTokens);           // start = 10 + 2
        Assert.Equal(0, report.ResponsesTotal);
        Assert.Equal(0, report.ResponseToSchemaRatio);
    }

    [Fact]
    public async Task Never_sends_a_dangling_tool_use_to_the_counter()
    {
        // count_tokens (HTTP 400) rejects a tool_use not immediately followed by a tool_result.
        // Regression guard: every message-bearing CountInput must be a valid paired turn.
        var counter = new CapturingCounter();
        await new SessionCostMeasurer(counter).MeasureAsync(Canonical(), Options);
        Assert.All(
            counter.Inputs.Where(i => i.Messages is not null),
            input => Assert.False(EndsWithDanglingToolUse(input.Messages)));
    }

    private static bool EndsWithDanglingToolUse(IReadOnlyList<TranscriptMessage>? messages)
    {
        if (messages is null || messages.Count == 0)
            return false;
        var last = messages[^1];
        var resultIds = last.Content.OfType<ToolResultBlock>().Select(b => b.ToolUseId).ToHashSet();
        return last.Content.OfType<ToolUseBlock>().Any(u => !resultIds.Contains(u.Id));
    }

    private sealed class CapturingCounter : ITokenCounter
    {
        public List<CountInput> Inputs { get; } = new();
        public MeasurementMode Mode => MeasurementMode.GroundTruth;
        public string Label => "cap";
        public Task<int> CountAsync(string model, CountInput input, CancellationToken ct = default)
        {
            Inputs.Add(input);
            return Task.FromResult(input.Messages?.Count ?? 0);
        }
    }
}
