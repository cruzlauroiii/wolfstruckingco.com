using System.Text.RegularExpressions;

namespace Scripts;

internal static partial class SharedPatterns
{
    [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
    internal static partial Regex ConstString();

    [GeneratedRegex(@"const\s+int\s+(?<name>\w+)\s*=\s*(?<value>-?\d+)\s*;", RegexOptions.ExplicitCapture)]
    internal static partial Regex ConstInt();
}

internal static class ScratchConfig
{
    public static async Task<Dictionary<string, string>> LoadStringsAsync(string ConfigPath)
    {
        var Body = await File.ReadAllTextAsync(ConfigPath);
        return SharedPatterns.ConstString().Matches(Body)
            .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);
    }

    public static async Task<Dictionary<string, int>> LoadIntsAsync(string ConfigPath)
    {
        var Body = await File.ReadAllTextAsync(ConfigPath);
        return SharedPatterns.ConstInt().Matches(Body)
            .ToDictionary(M => M.Groups["name"].Value, M => int.Parse(M.Groups["value"].Value, System.Globalization.CultureInfo.InvariantCulture), StringComparer.Ordinal);
    }

    public static Dictionary<string, string> ParseStrings(string Body)
    {
        return SharedPatterns.ConstString().Matches(Body)
            .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);
    }

    public static Dictionary<string, int> ParseInts(string Body)
    {
        return SharedPatterns.ConstInt().Matches(Body)
            .ToDictionary(M => M.Groups["name"].Value, M => int.Parse(M.Groups["value"].Value, System.Globalization.CultureInfo.InvariantCulture), StringComparer.Ordinal);
    }
}

internal static class Errors
{
    public static async Task<int> FailAsync(string Message, int Code)
    {
        await Console.Error.WriteLineAsync(Message);
        return Code;
    }
}
