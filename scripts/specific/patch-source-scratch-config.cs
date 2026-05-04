return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\rebuild-walkthrough-v3.cs";
        public const string Find_01 = "        if (Pad == \"001\")";
        public const string Replace_01 = "        if (Pad == \"058\" || Pad == \"059\")\n        {\n            var Fn = \"() => { var s = document.querySelector('.MapStageFull') || document.querySelector('.MapStage') || document.querySelector('.Stage'); if (s) { s.style.maxWidth = 'none'; s.style.padding = '0'; s.style.margin = '0'; s.style.width = '100vw'; s.style.height = 'calc(100vh - 60px)'; } var svg = document.querySelector('.MapSvg'); if (svg) { svg.style.width = '100%'; svg.style.height = '100%'; svg.style.display = 'block'; } return s ? 'expanded' : 'no-stage'; }\";\n            await Cdp(\"map-fullpage\", Eval(Fn));\n            await Task.Delay(800);\n        }\n        if (Pad == \"001\")";
    }
}
