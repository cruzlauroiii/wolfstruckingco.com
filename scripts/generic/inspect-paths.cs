#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Text.RegularExpressions;
using System.Net.Sockets;

if (args.Length < 1) { Console.Error.WriteLine("usage: dotnet run inspect-paths.cs config.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { Console.Error.WriteLine($"missing: {SpecPath}"); return 2; }
var Spec = await File.ReadAllTextAsync(SpecPath);
string Get(string Name)
{
    var Rx = new Regex("const\\s+string\\s+" + Name + "\\s*=\\s*@?\"((?:[^\"\\\\]|\\\\.)*)\"");
    var M = Rx.Match(Spec);
    return M.Success ? M.Groups[1].Value : "";
}

var Paths = Get("Paths").Split('|', StringSplitOptions.RemoveEmptyEntries);
var TempDirGlob = Get("TempDirGlob");
var Ports = Get("Ports").Split(',', StringSplitOptions.RemoveEmptyEntries);

foreach (var P in Paths)
{
    if (Directory.Exists(P)) Console.WriteLine($"DIR_EXISTS {P}");
    else if (File.Exists(P)) { var I = new FileInfo(P); var Body = ""; try { Body = (await File.ReadAllTextAsync(P)).Trim(); if (Body.Length > 80) Body = Body.Substring(0, 80) + "..."; } catch {} Console.WriteLine($"FILE_EXISTS size={I.Length} content=\"{Body}\" {P}"); }
    else Console.WriteLine($"MISSING {P}");
}

if (!string.IsNullOrEmpty(TempDirGlob))
{
    var Slash = TempDirGlob.LastIndexOfAny(new[] { '/', '\\' });
    var Root = Slash >= 0 ? TempDirGlob.Substring(0, Slash) : ".";
    var Pat = Slash >= 0 ? TempDirGlob.Substring(Slash + 1) : TempDirGlob;
    if (Directory.Exists(Root))
    {
        foreach (var F in Directory.GetFiles(Root, Pat))
        {
            var I = new FileInfo(F);
            Console.WriteLine($"GLOB {I.Length} {F}");
        }
    }
    else Console.WriteLine($"GLOB_ROOT_MISSING {Root}");
}

foreach (var Pr in Ports)
{
    var Port = int.Parse(Pr.Trim());
    using var Cl = new TcpClient();
    try
    {
        var Ct = Cl.ConnectAsync("127.0.0.1", Port);
        if (await Task.WhenAny(Ct, Task.Delay(1500)) == Ct && Cl.Connected) Console.WriteLine($"PORT_OPEN {Port}");
        else Console.WriteLine($"PORT_CLOSED {Port}");
    }
    catch { Console.WriteLine($"PORT_CLOSED {Port}"); }
}
return 0;
