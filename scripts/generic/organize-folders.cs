using System.IO;

const string Root = @"C:\repo\public\wolfstruckingco.com\main\scripts";
var Generic = Path.Combine(Root, "generic");
var Specific = Path.Combine(Root, "specific");
Directory.CreateDirectory(Generic);
Directory.CreateDirectory(Specific);

foreach (var Src in Directory.GetFiles(Root, "*.cs", SearchOption.TopDirectoryOnly))
{
    var Name = Path.GetFileName(Src);
    var Body = await File.ReadAllTextAsync(Src);
    var IsSpecific = Name.EndsWith("-config.cs", StringComparison.Ordinal) || Body.Contains("return 0;\n\nnamespace Scripts", StringComparison.Ordinal);
    var Dest = Path.Combine(IsSpecific ? Specific : Generic, Name);
    File.Move(Src, Dest, overwrite: true);
}
return 0;
