using System.IO;

const string Specific = @"C:\repo\public\wolfstruckingco.com\main\scripts\specific";
const string Generic = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic";
Directory.CreateDirectory(Generic);

foreach (var Src in Directory.GetFiles(Specific, "*.cs", SearchOption.TopDirectoryOnly))
{
    var Name = Path.GetFileName(Src);
    if (Name.EndsWith("-config.cs", StringComparison.Ordinal)) { continue; }
    File.Move(Src, Path.Combine(Generic, Name), overwrite: true);
}
return 0;
