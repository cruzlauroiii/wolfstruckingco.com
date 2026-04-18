#:property TargetFramework=net11.0

// inspect-inline-styles.cs - Specific. Owns the directories to scan.
// Counts inline `style="..."` per razor file; surfaces highest-density
// offenders to prioritise the next SCSS-conversion batch (#39).
const string Pages = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\";
const string Components = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\";

string[] Dirs = [Pages, Components];
var Findings = new List<(string Path, int Count)>();
foreach (var Dir in Dirs)
{
    foreach (var F in Directory.GetFiles(Dir, "*.razor", SearchOption.TopDirectoryOnly))
    {
        var Body = await File.ReadAllTextAsync(F);
        var Idx = 0; var Count = 0;
        while ((Idx = Body.IndexOf("style=\"", Idx, StringComparison.Ordinal)) >= 0) { Count++; Idx += 7; }
        if (Count > 0) { Findings.Add((F, Count)); }
    }
}
foreach (var (Path, Count) in Findings.OrderByDescending(F => F.Count))
{
    await Console.Out.WriteLineAsync($"{Count.ToString(System.Globalization.CultureInfo.InvariantCulture),4}  {Path[Pages.Length..]}");
}
await Console.Out.WriteLineAsync($"--- total: {Findings.Sum(F => F.Count).ToString(System.Globalization.CultureInfo.InvariantCulture)} inline style attributes across {Findings.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)} files ---");
return 0;
