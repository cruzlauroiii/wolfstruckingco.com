#:property TargetFramework=net11.0
#:project ../src/SharedUI/SharedUI.csproj
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SharedUI.Components;

var Repo = args.FirstOrDefault(A => Directory.Exists(A))
    ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
var DocsRoot = Path.Combine(Repo, "docs");

var Asm = typeof(MainLayout).Assembly;
var Routes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "videos", "app", "Generated" };
#pragma warning disable IL2026
foreach (var T in Asm.GetTypes())
{
    foreach (var R in T.GetCustomAttributes<RouteAttribute>())
    {
        var Slug = R.Template.Trim('/').Split('/')[0];
        if (Slug.Length > 0) { Routes.Add(Slug); }
    }
}
#pragma warning restore IL2026

var Removed = 0;
foreach (var Dir in Directory.EnumerateDirectories(DocsRoot))
{
    var Name = Path.GetFileName(Dir);
    if (Routes.Contains(Name)) { continue; }
    Directory.Delete(Dir, recursive: true);
    Removed++;
}

if (Removed > 0) { await Console.Out.WriteLineAsync($"removed {Removed.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); }
return 0;
