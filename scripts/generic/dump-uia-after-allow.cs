#:property TargetFramework=net11.0-windows
#:property UseWindowsForms=true
#:property UseWPF=true
#:property PublishTrimmed=false
#:property IsTrimmable=false
#:property PublishAot=false
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Automation;

const string Repo = @"C:\repo\public\wolfstruckingco.com\main";

var Psi = new ProcessStartInfo("dotnet", $"run \"{Repo}\\scripts\\cdp-allow.cs\"")
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    WorkingDirectory = Repo,
};
using (var P = Process.Start(Psi)!)
{
    Console.WriteLine("--- cdp-allow stdout ---");
    Console.WriteLine(await P.StandardOutput.ReadToEndAsync());
    Console.WriteLine("--- cdp-allow stderr ---");
    Console.WriteLine(await P.StandardError.ReadToEndAsync());
    await P.WaitForExitAsync();
}

await Task.Delay(200);

var Root = AutomationElement.RootElement;
var ChromeCondition = new PropertyCondition(AutomationElement.ClassNameProperty, "Chrome_WidgetWin_1");
var Windows = Root.FindAll(TreeScope.Children, ChromeCondition);
Console.WriteLine($"\nChrome windows found: {Windows.Count}");
foreach (AutomationElement Window in Windows)
{
    var Wr = Window.Current.BoundingRectangle;
    Console.WriteLine($"\n=== Window {Window.Current.Name} rect=({(int)Wr.X},{(int)Wr.Y} {(int)Wr.Width}x{(int)Wr.Height}) ===");
    var ButtonCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
    foreach (AutomationElement Button in Window.FindAll(TreeScope.Descendants, ButtonCondition))
    {
        var Name = Button.Current.Name;
        var AutoId = Button.Current.AutomationId;
        var ClassName = Button.Current.ClassName;
        var Rect = Button.Current.BoundingRectangle;
        if (Rect.IsEmpty) continue;
        var RelY = Rect.Y - Wr.Y;
        if (RelY > 350) continue;
        Console.WriteLine($"  btn name='{Name}' autoid='{AutoId}' class='{ClassName}' rect=({(int)Rect.X},{(int)Rect.Y} {(int)Rect.Width}x{(int)Rect.Height}) relY={(int)RelY}");
    }
}
return 0;
