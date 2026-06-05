using Microsoft.ML.Tokenizers;
using Xunit;

namespace ContextTax.Core.Tests;

public class O200kTokenizerTests
{
    [Fact]
    public void Loads_offline_and_counts_tokens()
    {
        var tokenizer = TiktokenTokenizer.CreateForEncoding("o200k_base");
        Assert.True(tokenizer.CountTokens("hello world") > 0);
    }

    [Fact]
    public void Is_deterministic()
    {
        var tokenizer = TiktokenTokenizer.CreateForEncoding("o200k_base");
        Assert.Equal(
            tokenizer.CountTokens("the quick brown fox"),
            tokenizer.CountTokens("the quick brown fox"));
    }
}
