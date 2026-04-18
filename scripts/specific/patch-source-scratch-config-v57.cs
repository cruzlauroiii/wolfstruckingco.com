return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV57
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\generate-statics.cs";

        public const string Find_01 = "Services.AddSingleton<WolfsInteropService>();\nServices.AddSingleton<VoiceChatService>();\nServices.AddSingleton<NavigationManager>";
        public const string Replace_01 = "Services.AddSingleton<WolfsInteropService>();\nServices.AddSingleton<NavigationManager>";

        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
