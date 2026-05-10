return 0;

namespace Scripts
{
    internal static class PatchSourceV4ReadyWaitTimeoutScratchConfigV1
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\rebuild-walkthrough-v4.cs";
        public const string Find_01 = "if (!string.IsNullOrEmpty(CachedWolfsPageIdx)) { for (int RIter = 0; RIter < 30; RIter++) { var ReadyEval = await CdpRead(\"ready-chk\", \"public const string Command = \\u0022evaluate_script\\u0022;\\n        public const string PageId = \\u0022\" + CachedWolfsPageIdx + \"\\u0022;\\n        public const string Function = \\u0022() => document.readyState.charCodeAt(0) === 99 && typeof window.Blazor !== typeof undefined\\u0022;\"); if (ReadyEval.Contains(\"true\", StringComparison.Ordinal)) break; await Task.Delay(1000); } }";
        public const string Replace_01 = "if (!string.IsNullOrEmpty(CachedWolfsPageIdx)) { for (int RIter = 0; RIter < 10; RIter++) { var EvalTask = CdpRead(\"rdy\", \"public const string Command = \\u0022evaluate_script\\u0022;\\n        public const string PageId = \\u0022\" + CachedWolfsPageIdx + \"\\u0022;\\n        public const string Function = \\u0022() => document.readyState.length === 8\\u0022;\"); var TimTask = Task.Delay(2000); if (await Task.WhenAny(EvalTask, TimTask) != EvalTask) { await Task.Delay(500); continue; } if ((await EvalTask).Contains(\"true\", StringComparison.Ordinal)) break; await Task.Delay(500); } }";
    }
}
