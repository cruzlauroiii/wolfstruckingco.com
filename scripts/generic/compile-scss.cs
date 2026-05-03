#:property TargetFramework=net11.0
using System.Diagnostics;

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strs = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (var Line in Body.Split('\n'))
{
    var Trimmed = Line.TrimStart();
    var ConstIdx = Trimmed.IndexOf("const string ", StringComparison.Ordinal);
    if (ConstIdx < 0) { continue; }
    var After = Trimmed[(ConstIdx + 13)..];
    var EqIdx = After.IndexOf(" = ", StringComparison.Ordinal);
    if (EqIdx < 0) { continue; }
    var Name = After[..EqIdx].Trim();
    var Rhs = After[(EqIdx + 3)..].TrimStart();
    if (Rhs.StartsWith('@')) { Rhs = Rhs[1..]; }
    if (!Rhs.StartsWith('\"')) { continue; }
    var EndQuote = Rhs.LastIndexOf("\";", StringComparison.Ordinal);
    if (EndQuote < 1) { continue; }
    Strs[Name] = Rhs[1..EndQuote];
}

if (!Strs.TryGetValue("Entry", out var Entry)) { return 3; }
if (!Strs.TryGetValue("Output", out var Output)) { return 4; }
if (!File.Exists(Entry)) { return 5; }
var OutDir = Path.GetDirectoryName(Output);
if (!string.IsNullOrEmpty(OutDir) && !Directory.Exists(OutDir)) { Directory.CreateDirectory(OutDir); }
var Style = Strs.TryGetValue("Style", out var StyleVal) ? StyleVal : "compressed";

var Psi = new ProcessStartInfo
{
    FileName = OperatingSystem.IsWindows() ? "sass.cmd" : "sass",
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
};
Psi.ArgumentList.Add(Entry);
Psi.ArgumentList.Add(Output);
Psi.ArgumentList.Add($"--style={Style}");
Psi.ArgumentList.Add("--no-source-map");

try
{
    using var Proc = Process.Start(Psi);
    if (Proc is null) { return 6; }
    var ReadOut = Proc.StandardOutput.ReadToEndAsync();
    var ReadErr = Proc.StandardError.ReadToEndAsync();
    await Proc.WaitForExitAsync();
    var OutText = await ReadOut;
    var ErrText = await ReadErr;
    if (Proc.ExitCode != 0)
    {
        await Console.Error.WriteAsync(ErrText);
        await Console.Error.WriteAsync(OutText);
    }
    return Proc.ExitCode;
}
catch (System.ComponentModel.Win32Exception)
{
    await Console.Error.WriteLineAsync("sass not on PATH (npm install -g sass)");
    return 7;
}
