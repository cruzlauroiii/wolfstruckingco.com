#:property TargetFramework=net11.0

using System.ComponentModel;
using System.Diagnostics;

if (args.Length < 1) { return 1; }
if (!System.IO.File.Exists(args[0])) { return 2; }

foreach (var P in Process.GetProcessesByName("chrome"))
{
    try { P.Kill(true); }
    catch (InvalidOperationException) { }
    catch (Win32Exception) { }
}
await Task.Delay(1500);
return 0;
