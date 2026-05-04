#:property TargetFramework=net11.0

using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { return 1; }
var Spec = args[0];
if (!System.IO.File.Exists(Spec)) { return 2; }

var Body = await System.IO.File.ReadAllTextAsync(Spec);
string SFromCfg(string Name, string Default)
{
    var V = TtsEdgePatterns.VerbatimConst().Match(Body);
    while (V.Success)
    {
        if (V.Groups["name"].Value == Name) { return V.Groups["v"].Value.Replace("\"\"", "\""); }
        V = V.NextMatch();
    }
    var R = TtsEdgePatterns.RegularConst().Match(Body);
    while (R.Success)
    {
        if (R.Groups["name"].Value == Name) { return Regex.Unescape(R.Groups["v"].Value); }
        R = R.NextMatch();
    }
    return Default;
}

var Voice = SFromCfg("Voice", "en-US-JennyNeural");
var Text = SFromCfg("Text", string.Empty);
var Output = SFromCfg("OutputPath", string.Empty);
var Rate = SFromCfg("Rate", "+0%");
var Pitch = SFromCfg("Pitch", "+0Hz");

if (string.IsNullOrEmpty(Text)) { await Console.Error.WriteLineAsync("Text const required"); return 3; }
if (string.IsNullOrEmpty(Output)) { await Console.Error.WriteLineAsync("OutputPath const required"); return 4; }

var Psi = new ProcessStartInfo("python")
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
};
Psi.ArgumentList.Add("-m");
Psi.ArgumentList.Add("edge_tts");
Psi.ArgumentList.Add("--voice");
Psi.ArgumentList.Add(Voice);
Psi.ArgumentList.Add("--rate");
Psi.ArgumentList.Add(Rate);
Psi.ArgumentList.Add("--pitch");
Psi.ArgumentList.Add(Pitch);
Psi.ArgumentList.Add("--text");
Psi.ArgumentList.Add(Text);
Psi.ArgumentList.Add("--write-media");
Psi.ArgumentList.Add(Output);

using var P = Process.Start(Psi)!;
var StdErr = await P.StandardError.ReadToEndAsync();
await P.WaitForExitAsync();
if (P.ExitCode != 0) { await Console.Error.WriteLineAsync(StdErr.Trim()); }
return P.ExitCode;

namespace Scripts
{
    internal static partial class TtsEdgePatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@"(?<v>(?:[^"]|"")*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex VerbatimConst();

        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*"(?<v>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex RegularConst();
    }
}
