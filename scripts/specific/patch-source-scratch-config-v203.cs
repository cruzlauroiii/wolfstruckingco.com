return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV203
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\chrome-devtools.cs";

        public const string Find_01 = "        StartInfo.ArgumentList.Add(\"--start-maximized\");\n        StartInfo.ArgumentList.Add(\"--remote-allow-origins=*\");\n        StartInfo.ArgumentList.Add(\"--disable-blink-features=AutomationControlled\");\n        Process.Start(StartInfo);";
        public const string Replace_01 = "        StartInfo.ArgumentList.Add(\"--start-maximized\");\n        StartInfo.ArgumentList.Add(\"--remote-allow-origins=*\");\n        Process.Start(StartInfo);";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
