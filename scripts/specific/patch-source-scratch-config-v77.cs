return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV77
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\build-all-scenes.cs";

        public const string Find_01 = "var VoiceRotation = new[]\n{\n    \"en-US-AnaNeural\",\n    \"en-GB-MaisieNeural\",\n    \"en-US-EmmaMultilingualNeural\",\n    \"en-US-AvaMultilingualNeural\",\n    \"en-US-JennyNeural\",\n    \"en-US-AriaNeural\",\n};";
        public const string Replace_01 = "var VoiceRotation = new[]\n{\n    \"en-US-AnaNeural\",\n    \"en-US-AndrewMultilingualNeural\",\n    \"ja-JP-NanamiNeural\",\n    \"en-US-BrianNeural\",\n    \"en-US-AvaMultilingualNeural\",\n    \"ja-JP-KeitaNeural\",\n    \"en-US-JennyNeural\",\n    \"en-US-EricNeural\",\n    \"en-US-AriaNeural\",\n    \"en-US-GuyNeural\",\n};";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
