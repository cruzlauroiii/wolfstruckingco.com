return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV216
    {
        public const string TargetFile = "main/scripts/generic/count-lines.cs";

        public const string Find_01 = "var Lines = (await File.ReadAllLinesAsync(FilePath)).Length;\nawait Console.Out.WriteLineAsync($\"{Lines.ToString(System.Globalization.CultureInfo.InvariantCulture)}\\t{Path.GetFileName(FilePath)}\");\nreturn 0;";
        public const string Replace_01 = "var Lines = (await File.ReadAllLinesAsync(FilePath)).Length;\nawait Console.Out.WriteLineAsync($\"{Lines.ToString(System.Globalization.CultureInfo.InvariantCulture)}\\t{Path.GetFileName(FilePath)}\");\nreturn 0;\n";
    }
}
