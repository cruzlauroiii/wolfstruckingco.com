#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:project ../src/SharedUI/SharedUI.csproj

// remove-orphans.cs — delete docs/<folder>/index.html files that don't
// correspond to a SharedUI Razor route. Keeps the public site as a pure
// projection of SharedUI: no legacy static pages drift around with stale
// content / technical references / etc.
//
//   dotnet run scripts/remove-orphans.cs                  # default repo
//   dotnet run scripts/remove-orphans.cs -- C:\…\main      # explicit repo

using System.Reflection;
using Microsoft.AspNetCore.Components;
using SharedUI.Components;

var Repo = args.FirstOrDefault(A => Directory.Exists(A))
    ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
var DocsRoot = Path.Combine(Repo, "docs");

var Asm = typeof(MainLayout).Assembly;
var Routes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "videos", "app", "Generated" };
foreach (var T in Asm.GetTypes())
{
    foreach (var R in T.GetCustomAttributes<RouteAttribute>())
    {
        var Slug = R.Template.Trim('/').Split('/')[0];
        if (Slug.Length > 0)
        {
            Routes.Add(Slug);
        }
    }
}

Console.WriteLine($"recognized routes: {string.Join(", ", Routes.OrderBy(R => R))}");
Console.WriteLine();

var Removed = 0;
foreach (var Dir in Directory.EnumerateDirectories(DocsRoot))
{
    var Name = Path.GetFileName(Dir);
    if (Routes.Contains(Name))
    {
        continue;
    }
    Console.WriteLine($"  ✗ orphan: docs/{Name}");
    Directory.Delete(Dir, recursive: true);
    Removed++;
}

Console.WriteLine();
Console.WriteLine($"removed {Removed} orphan folder(s) under docs/");
return 0;
