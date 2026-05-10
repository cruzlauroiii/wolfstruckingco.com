return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\tunnel-client.cs";
        public const string Find_01 = "    }\n    catch (Exception E) { await Console.Error.WriteLineAsync(\"auto-sync: \" + E.Message); }\n}";
        public const string Replace_01 = "    }\n}";
    }
}
