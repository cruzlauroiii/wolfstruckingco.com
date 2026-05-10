return 0;

namespace Scripts
{
    internal static class PatchSourceV4PadFieldConfigV1
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\rebuild-walkthrough-v4.cs";
        public const string Find_01 = "string PadFor(JsonElement Scene, int Idx)\n{\n    var T = Scene.GetProperty(\"target\").GetString() ?? \"\";\n    if (T.Contains(\"cb=\")) { var Cb = T.Split(\"cb=\")[^1].Replace(\"?\", \"\").Replace(\"/\", \"\").Trim(); return Cb.Substring(0, Math.Min(3, Cb.Length)); }\n    return Idx.ToString(\"D3\");\n}";
        public const string Replace_01 = "string PadFor(JsonElement Scene, int Idx)\n{\n    if (Scene.TryGetProperty(\"pad\", out var PadEl) && PadEl.ValueKind == JsonValueKind.String) { var Pp = PadEl.GetString(); if (!string.IsNullOrEmpty(Pp)) return Pp; }\n    var T = Scene.GetProperty(\"target\").GetString() ?? \"\";\n    if (T.Contains(\"cb=\")) { var Cb = T.Split(\"cb=\")[^1].Replace(\"?\", \"\").Replace(\"/\", \"\").Trim(); return Cb.Substring(0, Math.Min(4, Cb.Length)); }\n    return Idx.ToString(\"D3\");\n}";
    }
}
