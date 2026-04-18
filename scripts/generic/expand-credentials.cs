#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include ../specific/expand-credentials-config.cs
using System.Text.Json.Nodes;
using Scripts;

if (args.Length == 0) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/expand-credentials.cs -- <scenes.json>"); return 1; }
var Path = args[0];
if (!File.Exists(Path)) { await Console.Error.WriteLineAsync($"missing: {Path}"); return 1; }

var Body = await File.ReadAllTextAsync(Path);
var Arr = JsonNode.Parse(Body)!.AsArray();

if (Arr.Count >= ExpandCredentialsConfig.SceneIndex)
{
    var Scene = Arr[ExpandCredentialsConfig.SceneIndex - 1]!.AsObject();
    Scene["target"] = ExpandCredentialsConfig.NewTarget;
}
await File.WriteAllTextAsync(Path, Arr.ToJsonString(new() { WriteIndented = true }));
return 0;

