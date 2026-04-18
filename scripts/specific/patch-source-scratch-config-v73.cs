return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV73
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\build-all-scenes.cs";

        public const string Find_01 = "#:property TargetFramework=net11.0\n#:property RunAnalyzersDuringBuild=false\n#:property TreatWarningsAsErrors=false\n#:property EnforceCodeStyleInBuild=false\n\nusing System;";
        public const string Replace_01 = "#:property TargetFramework=net11.0\n#:property RunAnalyzersDuringBuild=false\n#:property TreatWarningsAsErrors=false\n#:property EnforceCodeStyleInBuild=false\n#:property ExperimentalFileBasedProgramEnableIncludeDirective=true\n#:include pipeline-scene-config.cs\n\nusing System;";

        public const string Find_02 = "var Failures = 0;\nfor (var N = 1; N <= Scenes.Length; N++)\n{";
        public const string Replace_02 = "var Failures = 0;\nvar SceneStart = Math.Max(1, VideoPipeline.PipelineSceneConfig.Start);\nvar SceneEnd = Math.Min(Scenes.Length, VideoPipeline.PipelineSceneConfig.End);\nfor (var N = SceneStart; N <= SceneEnd; N++)\n{";

        public const string Find_03 = "    var Mp4 = Path.Combine(Repo, \"docs\", \"videos\", \"scene-\" + Pad + \".mp4\");\n    var FfExit = await RunAsync(\n        \"ffmpeg\",\n        \"-y\",\n        \"-loop\", \"1\",\n        \"-i\", Png,\n        \"-i\", Mp3,\n        \"-pix_fmt\", \"yuv420p\",\n        \"-vf\", \"fps=30,scale=trunc(iw/2)*2:trunc(ih/2)*2\",\n        \"-r\", \"30\",\n        \"-c:v\", \"libx264\",\n        \"-preset\", \"medium\",\n        \"-crf\", \"22\",\n        \"-movflags\", \"+faststart\",\n        \"-c:a\", \"aac\",\n        \"-b:a\", \"128k\",\n        \"-shortest\",\n        Mp4);";
        public const string Replace_03 = "    var Mp4 = Path.Combine(Repo, \"docs\", \"videos\", \"scene-\" + Pad + \".mp4\");\n    var FfExit = await RunAsync(\n        \"ffmpeg\",\n        \"-y\",\n        \"-loop\", \"1\",\n        \"-framerate\", \"30\",\n        \"-i\", Png,\n        \"-i\", Mp3,\n        \"-c:v\", \"libx264\",\n        \"-tune\", \"stillimage\",\n        \"-pix_fmt\", \"yuv420p\",\n        \"-preset\", \"medium\",\n        \"-crf\", \"22\",\n        \"-g\", \"30\",\n        \"-vf\", \"pad=ceil(iw/2)*2:ceil(ih/2)*2\",\n        \"-c:a\", \"aac\",\n        \"-b:a\", \"192k\",\n        \"-movflags\", \"+faststart\",\n        \"-shortest\",\n        Mp4);";

        public const string Find_04 = "___UNUSED_SLOT___";
        public const string Replace_04 = "";
    }
}
