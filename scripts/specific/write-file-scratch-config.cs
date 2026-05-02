return 0;

namespace Scripts
{
    internal static class WriteFileScratchConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\py-run.cs";
        public const string Content = "#:property TargetFramework=net11.0\n#:property RunAnalyzersDuringBuild=false\n#:property TreatWarningsAsErrors=false\n#:property EnforceCodeStyleInBuild=false\nusing System.Diagnostics;\n\nif (args.Length < 1) return 1;\nvar SpecPath = args[0];\nif (!File.Exists(SpecPath)) return 2;\n\nvar Specs = await File.ReadAllLinesAsync(SpecPath);\nstring? Read(string Name)\n{\n    foreach (var Line in Specs)\n    {\n        var Idx = Line.IndexOf(\"const string \" + Name + \" = \", StringComparison.Ordinal);\n        if (Idx < 0) continue;\n        var After = Line.Substring(Idx + 13 + Name.Length + 3);\n        if (After.StartsWith(\"@\", StringComparison.Ordinal)) After = After.Substring(1);\n        if (!After.StartsWith(\"\\\"\", StringComparison.Ordinal)) continue;\n        var End = After.LastIndexOf(\"\\\";\", StringComparison.Ordinal);\n        if (End < 1) continue;\n        return After.Substring(1, End - 1);\n    }\n    return null;\n}\n\nvar PyScript = Read(\"PyScript\");\nif (PyScript is null) return 3;\nif (!File.Exists(PyScript)) return 4;\n\nvar Psi = new ProcessStartInfo(\"python\") { UseShellExecute = false };\nPsi.ArgumentList.Add(PyScript);\nusing var P = Process.Start(Psi)!;\nawait P.WaitForExitAsync();\nreturn P.ExitCode;\n";
        public const string Mode = "overwrite";
    }
}
