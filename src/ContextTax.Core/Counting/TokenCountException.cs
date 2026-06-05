namespace ContextTax.Core.Counting;

public sealed class TokenCountException : Exception
{
    public int StatusCode { get; }

    public TokenCountException(int statusCode, string responseBody)
        : base($"count_tokens failed (HTTP {statusCode}): {responseBody}")
        => StatusCode = statusCode;
}
