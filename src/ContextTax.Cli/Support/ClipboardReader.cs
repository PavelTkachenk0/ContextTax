using System.Diagnostics;

namespace ContextTax.Cli.Support;

/// <summary>Reads the OS clipboard (CLI-layer I/O). macOS only for now (via <c>pbpaste</c>); other
/// platforms return a friendly message pointing at a file or a pipe. Returns (null, message) on any
/// failure so callers can show it and continue.</summary>
public static class ClipboardReader
{
    public static (string? Text, string? Error) Read()
    {
        if (!OperatingSystem.IsMacOS())
            return (null, "reading the clipboard is supported on macOS only — use a file, or pipe the text via stdin.");

        try
        {
            using var process = Process.Start(new ProcessStartInfo("pbpaste")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
            });
            if (process is null)
                return (null, "could not start pbpaste to read the clipboard.");

            var text = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return string.IsNullOrWhiteSpace(text) ? (null, "the clipboard is empty.") : (text, null);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return (null, $"could not read the clipboard: {ex.Message}");
        }
    }
}
