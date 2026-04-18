return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV13
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";
        public const string Find_01 = "const string Base = \"https://cruzlauroiii.github.io/wolfstruckingco.com\";";
        public const string Replace_01 = "const int LocalPort = 8444;\r\nconst string Base = \"http://127.0.0.1:8444/wolfstruckingco.com\";\r\nvar PipelineDocsRoot = Path.Combine(Repo, \"docs\");\r\nvar PipelineListener = new System.Net.HttpListener();\r\nPipelineListener.Prefixes.Add(\"http://127.0.0.1:8444/\");\r\nPipelineListener.Start();\r\nvar PipelineListenerCts = new CancellationTokenSource();\r\n_ = Task.Run(async () => { while (!PipelineListenerCts.IsCancellationRequested) { System.Net.HttpListenerContext Ctx; try { Ctx = await PipelineListener.GetContextAsync(); } catch { return; } var P1 = Ctx.Request.Url?.AbsolutePath ?? \"/\"; const string Pfx = \"/wolfstruckingco.com\"; if (P1.StartsWith(Pfx, StringComparison.Ordinal)) { P1 = P1[Pfx.Length..]; } if (string.IsNullOrEmpty(P1) || P1 == \"/\") { P1 = \"/index.html\"; } else if (P1.EndsWith(\"/\", StringComparison.Ordinal)) { P1 += \"index.html\"; } var Fp = Path.Combine(PipelineDocsRoot, P1.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)); if (File.Exists(Fp)) { var Bts = await File.ReadAllBytesAsync(Fp); var Ext = Path.GetExtension(Fp).ToLowerInvariant(); Ctx.Response.ContentType = Ext switch { \".html\" => \"text/html; charset=utf-8\", \".css\" => \"text/css\", \".js\" => \"application/javascript\", \".json\" => \"application/json\", \".png\" => \"image/png\", \".wasm\" => \"application/wasm\", _ => \"application/octet-stream\" }; Ctx.Response.Headers.Add(\"Cache-Control\", \"no-store\"); await Ctx.Response.OutputStream.WriteAsync(Bts); } else { Ctx.Response.StatusCode = 404; } try { Ctx.Response.Close(); } catch { } } });\r\nConsole.WriteLine(\"local pipeline server: \" + Base);";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
