return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV15
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";
        public const string Find_01 = "const int LocalPort = 8444;\r\nconst string Base = \"http://127.0.0.1:8444/wolfstruckingco.com\";";
        public const string Replace_01 = "const string Base = \"https://cruzlauroiii.github.io/wolfstruckingco.com\";";
        public const string Find_02 = "var PipelineDocsRoot = Path.Combine(Repo, \"docs\");";
        public const string Replace_02 = "";
        public const string Find_03 = "var PipelineListener = new System.Net.HttpListener();";
        public const string Replace_03 = "";
        public const string Find_04 = "PipelineListener.Prefixes.Add(\"http://127.0.0.1:8444/\");";
        public const string Replace_04 = "";
        public const string Find_05 = "PipelineListener.Start();";
        public const string Replace_05 = "";
        public const string Find_06 = "var PipelineListenerCts = new CancellationTokenSource();";
        public const string Replace_06 = "";
        public const string Find_07 = "Console.WriteLine(\"local pipeline server: \" + Base);";
        public const string Replace_07 = "";
    }
}
