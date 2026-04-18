using System.IO;
using System.Text.RegularExpressions;

namespace CdpTool;

public static partial class ScratchConfigParser
{
    private const string ConstStringKey = "name";
    private const string ConstStringValue = "value";

    public static string[] Expand(string ConfigPath)
    {
        var Body = File.ReadAllText(ConfigPath);
        var Strs = ConstStringRegex().Matches(Body)
            .ToDictionary(M => M.Groups[ConstStringKey].Value, M => M.Groups[ConstStringValue].Value, StringComparer.Ordinal);
        var Ints = ConstIntRegex().Matches(Body)
            .ToDictionary(M => M.Groups[ConstStringKey].Value, M => M.Groups[ConstStringValue].Value, StringComparer.Ordinal);
        var Result = new List<string>();
        if (Strs.TryGetValue("Command", out var Command))
        {
            Result.Add(Command);
            Strs.Remove("Command");
        }

        foreach (var Pair in Strs)
        {
            Result.Add("--" + char.ToLowerInvariant(Pair.Key[0]) + Pair.Key[1..]);
            Result.Add(Pair.Value);
        }

        foreach (var Pair in Ints)
        {
            Result.Add("--" + char.ToLowerInvariant(Pair.Key[0]) + Pair.Key[1..]);
            Result.Add(Pair.Value);
        }

        return Result.ToArray();
    }

    [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
    private static partial Regex ConstStringRegex();

    [GeneratedRegex(@"const\s+int\s+(?<name>\w+)\s*=\s*(?<value>-?\d+)\s*;", RegexOptions.ExplicitCapture)]
    private static partial Regex ConstIntRegex();
}
