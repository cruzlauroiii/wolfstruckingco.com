return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\regen-worker-cs.cs";
        public const string Find_01 = "    Sb.Append(\"    \\\"\");\n    Sb.Append(B64.AsSpan(I, Len));\n    Sb.AppendLine(\"\\\",\");";
        public const string Replace_01 = "    Sb.Append(\"    \\\"\").Append(B64.AsSpan(I, Len)).AppendLine(\"\\\",\");";
    }
}
