return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfigV34
    {
        public const string Pattern = "WolfsInterop|WolfsChatVoice|<script|SsoSnippet|HeaderAuthSnippet";
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\scripts";
        public const string FilePattern = "*.cs";
        public const string OutputFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\specific\grep-out-v34.txt";
    }
}
