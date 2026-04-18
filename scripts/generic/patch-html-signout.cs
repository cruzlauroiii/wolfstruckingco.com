#:property TargetFramework=net11.0

using System;
using System.IO;
using System.Threading.Tasks;

if (args.Length < 1) { return 1; }
if (!File.Exists(args[0])) { return 2; }

const string Root = @"C:\repo\public\wolfstruckingco.com\main\docs";
const string ButtonOnclick = " onclick=\"['wolfs_role','wolfs_email','wolfs_session'].forEach(function(k){localStorage.removeItem(k);});location.replace('/wolfstruckingco.com/');\"";

const string LinkBtnFind = "<button type=\"button\" class=\"LinkBtn\">Sign out";
const string LinkBtnReplace = "<button type=\"button\" class=\"LinkBtn\"" + ButtonOnclick + ">Sign out";

const string GhostBtnFind = "<button class=\"Btn Ghost\">Sign out</button>";
const string GhostBtnReplace = "<button class=\"Btn Ghost\"" + ButtonOnclick + ">Sign out</button>";

var Patched = 0;
foreach (var Html in Directory.EnumerateFiles(Root, "*.html", SearchOption.AllDirectories))
{
    var Body = await File.ReadAllTextAsync(Html);
    var Original = Body;
    if (Body.Contains(LinkBtnFind, StringComparison.Ordinal) && !Body.Contains(LinkBtnReplace, StringComparison.Ordinal))
    {
        Body = Body.Replace(LinkBtnFind, LinkBtnReplace, StringComparison.Ordinal);
    }
    if (Body.Contains(GhostBtnFind, StringComparison.Ordinal) && !Body.Contains(GhostBtnReplace, StringComparison.Ordinal))
    {
        Body = Body.Replace(GhostBtnFind, GhostBtnReplace, StringComparison.Ordinal);
    }
    if (!ReferenceEquals(Body, Original) && Body != Original)
    {
        await File.WriteAllTextAsync(Html, Body);
        Patched++;
    }
}
return 0;
