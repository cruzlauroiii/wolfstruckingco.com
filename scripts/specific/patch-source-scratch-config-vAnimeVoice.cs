namespace WolfsTruckingCo.Scripts.Specific;

public static class PatchSourceScratchConfigVAnimeVoice
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\build-all-scenes.cs";
    public const string Find_01 = "const string Rate = \"+5%\";\nconst string Pitch = \"+0Hz\";";
    public const string Replace_01 = "const string Rate = \"+8%\";\nconst string Pitch = \"+50Hz\";";
    public const string Find_02 = "var VoiceRotation = new[]\n{\n    \"en-US-AnaNeural\",\n    \"en-US-AndrewMultilingualNeural\",\n    \"en-US-AvaMultilingualNeural\",\n    \"en-US-BrianNeural\",\n    \"en-US-JennyNeural\",\n    \"en-US-EricNeural\",\n    \"en-US-AriaNeural\",\n    \"en-US-GuyNeural\",\n    \"en-US-MichelleNeural\",\n    \"en-US-ChristopherNeural\",\n};";
    public const string Replace_02 = "var VoiceRotation = new[]\n{\n    \"en-US-AnaNeural\",\n    \"en-US-AvaMultilingualNeural\",\n    \"en-US-JennyNeural\",\n    \"en-US-AriaNeural\",\n    \"en-US-MichelleNeural\",\n    \"en-US-EmmaMultilingualNeural\",\n    \"en-US-BrianMultilingualNeural\",\n    \"en-US-AndrewMultilingualNeural\",\n    \"en-US-EricNeural\",\n    \"en-US-GuyNeural\",\n    \"en-US-ChristopherNeural\",\n    \"en-GB-SoniaNeural\",\n    \"en-GB-MaisieNeural\",\n    \"en-AU-NatashaNeural\",\n    \"en-AU-WilliamNeural\",\n};";
}
