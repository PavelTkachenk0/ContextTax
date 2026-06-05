namespace ContextTax.Core.Mcp;

public sealed class ToolsJsonException : Exception
{
    public ToolsJsonException(string message, Exception? inner = null) : base(message, inner) { }
}
