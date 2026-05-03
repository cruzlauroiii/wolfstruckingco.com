return 0;

namespace Scripts
{
    internal static class TtsRotateScratchConfig
    {
        public const string NarrationsPath = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\narrations.json";
        public const string AudioDir = @"C:\Users\user1\AppData\Local\Temp\wolfs-walkthrough\audio";
        public const string IndexOutputPath = @"C:\Users\user1\AppData\Local\Temp\wolfs-walkthrough\audio-index.json";

        public const string Engine0Name = @"coqui-vits";
        public const string Engine0Cmd = @"python C:\tools\GPT-SoVITS\inference_cli.py --text ""{text}"" --output ""{out}""";

        public const string Engine1Name = @"coqui-jenny";
        public const string Engine1Cmd = "python C:\\tools\\GPT-SoVITS\\inference_cli.py --text \"{text}\" --output \"{out}\"";

        public const string Engine2Name = @"coqui-tacotron";
        public const string Engine2Cmd = "tts --text \"{text}\" --model_name tts_models/multilingual/multi-dataset/xtts_v2 --speaker_wav C:\\tools\\refs\\anime-en.wav --language_idx en --out_path \"{out}\"";

        public const string Engine3Name = @"";
        public const string Engine3Cmd = "python C:\\tools\\OpenVoice\\openvoice_cli.py --text \"{text}\" --reference C:\\tools\\refs\\anime-en.wav --out \"{out}\"";

        public const string Engine4Name = @"";
        public const string Engine4Cmd = "python C:\\tools\\tortoise-tts\\do_tts.py --text \"{text}\" --voice anime --output_path \"{out}\"";
    }
}
