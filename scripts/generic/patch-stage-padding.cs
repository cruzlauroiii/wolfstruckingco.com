#:property TargetFramework=net11.0

// patch-stage-padding.cs - increase top padding on .Stage so the page topic
// content sits in the middle of video screenshots (item #4). Also tightens
// .Hero padding for the same reason. Idempotent — safe to re-run.
const string Path = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\wwwroot\css\app.css";
var Text = await File.ReadAllTextAsync(Path);

(string Old, string New)[] Patches =
[
    (".Stage{margin:0 auto;padding:20px 16px 60px;width:100%;max-width:1100px;flex:1}",
     ".Stage{margin:0 auto;padding:14vh 16px 12vh;width:100%;max-width:1100px;flex:1;display:flex;flex-direction:column;justify-content:flex-start}"),

    (".Hero{padding:36px 18px;text-align:center;background:linear-gradient(135deg, var(--card) 0%, #fff 100%);border-radius:var(--radius);margin-bottom:20px}",
     ".Hero{padding:8vh 18px;text-align:center;background:linear-gradient(135deg, var(--card) 0%, #fff 100%);border-radius:var(--radius);margin-bottom:20px}"),

    ("@media(max-width: 768px){.Stage{padding:16px 0 40px;max-width:100%}",
     "@media(max-width: 768px){.Stage{padding:10vh 0;max-width:100%}"),
];

var Total = 0;
foreach (var (Old, New) in Patches)
{
    if (!Text.Contains(Old, StringComparison.Ordinal)) { await Console.Error.WriteLineAsync($"miss: '{Old[..Math.Min(50, Old.Length)]}...'"); continue; }
    Text = Text.Replace(Old, New);
    Total++;
}
await File.WriteAllTextAsync(Path, Text);
await Console.Out.WriteLineAsync($"applied {Total.ToString(System.Globalization.CultureInfo.InvariantCulture)}/{Patches.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)} stage-padding patches");
return Total == Patches.Length ? 0 : 1;
