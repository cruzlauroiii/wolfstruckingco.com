return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\narration-vs-ocr.cs";
        public const string Find_01 = "    var TxtPath = Path.Combine(OcrDir, \"scene-\" + P + \".txt\");\n    var Ocr = File.Exists(TxtPath) ? await File.ReadAllTextAsync(TxtPath) : string.Empty;";
        public const string Replace_01 = "    var TxtPathPrefixed = Path.Combine(OcrDir, \"scene-\" + P + \".txt\");\n    var TxtPathBare = Path.Combine(OcrDir, P + \".txt\");\n    string TxtPath;\n    if (File.Exists(TxtPathBare) && File.Exists(TxtPathPrefixed))\n    {\n        TxtPath = new FileInfo(TxtPathBare).LastWriteTimeUtc > new FileInfo(TxtPathPrefixed).LastWriteTimeUtc ? TxtPathBare : TxtPathPrefixed;\n    }\n    else if (File.Exists(TxtPathBare)) { TxtPath = TxtPathBare; }\n    else { TxtPath = TxtPathPrefixed; }\n    var Ocr = File.Exists(TxtPath) ? await File.ReadAllTextAsync(TxtPath) : string.Empty;";
    }
}
