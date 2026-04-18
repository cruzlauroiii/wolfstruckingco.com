return 0;

namespace Scripts
{
    internal static class DumpFileScratchConfigV58
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\generate-statics.cs";
        public const string Pattern = "(?i)(VoiceChatService|using\\s+|Services\\.|AddSingleton|AddScoped|AddTransient|builder\\.|Provider\\.)";
    }
}
