using System.Reflection;

namespace ContextTax.Cli.Support;

/// <summary>The CLI's own version, sourced from the assembly's informational version (set by -p:Version).</summary>
public static class VersionInfo
{
    public static string Current =>
        Normalize(Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    /// <summary>Strip SourceLink build metadata ("1.2.3+sha" → "1.2.3"); default when absent.</summary>
    public static string Normalize(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
            return "0.0.0";
        var plus = informationalVersion.IndexOf('+');
        return plus >= 0 ? informationalVersion[..plus] : informationalVersion;
    }
}
