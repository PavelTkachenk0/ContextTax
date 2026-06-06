namespace ContextTax.Core.Transcript;

public sealed class TranscriptException : Exception
{
    public TranscriptException(string message, Exception? inner = null) : base(message, inner) { }
}
