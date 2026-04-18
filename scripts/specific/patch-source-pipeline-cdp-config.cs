return 0;

namespace Scripts
{
    internal static class PatchSourcePipelineCdpConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";
        public const string Find_01 = "var TargetsJson = JsonDocument.Parse(await Http.GetStringAsync($\"http://127.0.0.1:{CdpPort.ToString(System.Globalization.CultureInfo.InvariantCulture)}/json\"));";
        public const string Replace_01 = "var TargetsJson = JsonDocument.Parse(await Http.GetStringAsync($\"http://127.0.0.1:{CdpPort.ToString(System.Globalization.CultureInfo.InvariantCulture)}/json/list\"));";
    }
}
