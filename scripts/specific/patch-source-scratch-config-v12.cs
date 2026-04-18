return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV12
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";
        public const string Find_01 = "const string Repo = @\"C:\\repo\\public\\wolfstruckingco.com\\main\";\nconst string Base = \"https://cruzlauroiii.github.io/wolfstruckingco.com\";";
        public const string Replace_01 = "const string Repo = @\"C:\\repo\\public\\wolfstruckingco.com\\main\";\nconst int LocalPort = 8444;\nconst string Base = \"http://127.0.0.1:8444/wolfstruckingco.com\";\nvar DocsRoot = Path.Combine(Repo, \"docs\");\nvar Listener = new System.Net.HttpListener();\nListener.Prefixes.Add($\"http://127.0.0.1:{LocalPort.ToString(System.Globalization.CultureInfo.InvariantCulture)}/\");\nListener.Start();\nvar ListenerCts = new CancellationTokenSource();\n_ = Task.Run(async () =>\n{\n    while (!ListenerCts.IsCancellationRequested)\n    {\n        System.Net.HttpListenerContext Ctx;\n        try { Ctx = await Listener.GetContextAsync(); } catch { return; }\n        var Path1 = Ctx.Request.Url?.AbsolutePath ?? \"/\";\n        const string Prefix = \"/wolfstruckingco.com\";\n        if (Path1.StartsWith(Prefix, StringComparison.Ordinal)) { Path1 = Path1[Prefix.Length..]; }\n        if (string.IsNullOrEmpty(Path1) || Path1 == \"/\") { Path1 = \"/index.html\"; }\n        else if (Path1.EndsWith(\"/\", StringComparison.Ordinal)) { Path1 += \"index.html\"; }\n        var FilePath = Path.Combine(DocsRoot, Path1.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));\n        if (File.Exists(FilePath))\n        {\n            var Bytes = await File.ReadAllBytesAsync(FilePath);\n            var Ext = Path.GetExtension(FilePath).ToLowerInvariant();\n            Ctx.Response.ContentType = Ext switch { \".html\" => \"text/html; charset=utf-8\", \".css\" => \"text/css\", \".js\" => \"application/javascript\", \".json\" => \"application/json\", \".png\" => \"image/png\", \".jpg\" => \"image/jpeg\", \".svg\" => \"image/svg+xml\", \".wasm\" => \"application/wasm\", _ => \"application/octet-stream\" };\n            Ctx.Response.Headers.Add(\"Cache-Control\", \"no-store\");\n            await Ctx.Response.OutputStream.WriteAsync(Bytes);\n        }\n        else { Ctx.Response.StatusCode = 404; }\n        try { Ctx.Response.Close(); } catch { }\n    }\n});\nConsole.WriteLine($\"local pipeline server: {Base}\");";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
