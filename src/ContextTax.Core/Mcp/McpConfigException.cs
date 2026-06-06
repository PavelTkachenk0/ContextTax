namespace ContextTax.Core.Mcp;

public sealed class McpConfigException : Exception
{
    public McpConfigException(string message, Exception? inner = null) : base(message, inner) { }
}
