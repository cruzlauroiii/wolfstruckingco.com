#:property TargetFramework=net11.0

// patch-gitignore.cs - append generated/runtime artifact ignores so they never get
// staged. Idempotent (skip rule if already present).
const string Path = @"C:\repo\public\wolfstruckingco.com\main\.gitignore";
string[] Rules =
[
    "## Generated artifacts (added 2026-04-29)",
    "docs/Generated/",
    "docs/app/",
    "docs/videos/out/",
    "docs/videos/lighthouse-reports/",
    "docs/videos/ocr.json",
    "docs/videos/frame-references.md",
    "data/wolfs-db.jsonl",
    "slnx/",
    "wasm-publish/",
];

var Text = await File.ReadAllTextAsync(Path);
var Sb = new System.Text.StringBuilder(Text);
if (!Text.EndsWith('\n')) { Sb.Append('\n'); }
var Added = 0;
foreach (var R in Rules)
{
    if (R.StartsWith("##", StringComparison.Ordinal)) { Sb.Append('\n').Append(R).Append('\n'); continue; }
    if (Text.Contains(R + "\n", StringComparison.Ordinal) || Text.Contains(R + "\r\n", StringComparison.Ordinal)) { continue; }
    Sb.Append(R).Append('\n'); Added++;
}
if (Added == 0) { await Console.Out.WriteLineAsync("all rules already present"); return 0; }
await File.WriteAllTextAsync(Path, Sb.ToString());
await Console.Out.WriteLineAsync($"appended {Added.ToString(System.Globalization.CultureInfo.InvariantCulture)} rules");
return 0;
