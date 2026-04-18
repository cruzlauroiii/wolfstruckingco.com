return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV5
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\build-all-scenes.cs";
        public const string Find_01 = "const string Voice = \"en-US-AnaNeural\";\nconst string Rate = \"+5%\";\nconst string Pitch = \"+0Hz\";";
        public const string Replace_01 = "var VoiceRotation = new[]\n{\n    \"en-US-AnaNeural\",\n    \"en-GB-MaisieNeural\",\n    \"en-US-EmmaMultilingualNeural\",\n    \"en-US-AvaMultilingualNeural\",\n    \"en-US-JennyNeural\",\n    \"en-US-AriaNeural\",\n};\nconst string Rate = \"+5%\";\nconst string Pitch = \"+0Hz\";";
        public const string Find_02 = "    var TtsExit = await RunAsync(\n        \"python\",\n        \"-m\", \"edge_tts\",\n        \"--voice\", Voice,";
        public const string Replace_02 = "    var Voice = VoiceRotation[(N - 1) % VoiceRotation.Length];\n    var TtsExit = await RunAsync(\n        \"python\",\n        \"-m\", \"edge_tts\",\n        \"--voice\", Voice,";
    }
}
