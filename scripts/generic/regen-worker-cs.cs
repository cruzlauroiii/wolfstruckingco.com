#:property TargetFramework=net11.0

using System.Text;

if (args.Length < 1)
{
    return 1;
}

var Specs = await File.ReadAllLinesAsync(args[0]);

string Get(string Name)
{
    foreach (var Line in Specs)
    {
        var Pat = "const string " + Name + " = ";
        var At = Line.IndexOf(Pat, StringComparison.Ordinal);
        if (At < 0)
        {
            continue;
        }

        var Tail = Line[(At + Pat.Length)..];
        if (Tail.Length > 0 && Tail[0] == '@')
        {
            Tail = Tail[1..];
        }

        if (Tail.Length == 0 || Tail[0] != '\u0022')
        {
            continue;
        }

        var End = Tail.LastIndexOf("\u0022;", StringComparison.Ordinal);
        if (End < 1)
        {
            continue;
        }

        return Tail[1..End];
    }

    return string.Empty;
}

var JsPath = Get("JsPath");
var CsPath = Get("CsPath");

if (!File.Exists(JsPath))
{
    return 2;
}

var Bytes = await File.ReadAllBytesAsync(JsPath);
var B64 = Convert.ToBase64String(Bytes);

var Sb = new StringBuilder();
Sb.AppendLine("#:property TargetFramework=net11.0");
Sb.AppendLine("#:property RunAnalyzersDuringBuild=false");
Sb.AppendLine("#:property TreatWarningsAsErrors=false");
Sb.AppendLine("#:property EnforceCodeStyleInBuild=false");
Sb.AppendLine();
Sb.Append("const string Path = \"");
Sb.Append(JsPath.Replace("\\", "\\\\", StringComparison.Ordinal));
Sb.AppendLine("\";");
Sb.AppendLine("var B64 = $\"{string.Join(string.Empty, JsBody())}\";");
Sb.AppendLine("var Bytes = Convert.FromBase64String(B64);");
Sb.AppendLine("File.WriteAllBytes(Path, Bytes);");
Sb.AppendLine("return 0;");
Sb.AppendLine();
Sb.AppendLine("static string[] JsBody() => new[]");
Sb.AppendLine("{");

var ChunkSize = 100;
for (var I = 0; I < B64.Length; I += ChunkSize)
{
    var Len = Math.Min(ChunkSize, B64.Length - I);
    Sb.Append("    \"").Append(B64.AsSpan(I, Len)).AppendLine("\",");
}

Sb.AppendLine("};");

await File.WriteAllTextAsync(CsPath, Sb.ToString());
return 0;
