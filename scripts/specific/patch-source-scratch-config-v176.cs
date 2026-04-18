return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV176
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";

        public const string Find_01 = "    Console.WriteLine($\"  → scene {Pad} {Route} navigating\");\n    var ScenePnfFinal = false;\n    for (var Attempt = 1; Attempt <= 3; Attempt++)";
        public const string Replace_01 = "    Console.WriteLine($\"  → scene {Pad} {Route} navigating\");\n    for (var Attempt = 1; Attempt <= 3; Attempt++)";

        public const string Find_02 = "            ScenesPnf.Add(Pad);\n            ScenePnfFinal = true;\n            Console.Error.WriteLine($\"    ✗ pnf gate: {Pad} {Route} still PNF after 3 attempts\");";
        public const string Replace_02 = "            ScenesPnf.Add(Pad);\n            Console.Error.WriteLine($\"    ✗ pnf gate: {Pad} {Route} still PNF after 3 attempts\");";
    }
}
