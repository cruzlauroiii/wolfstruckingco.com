return 0;

namespace Scripts
{
    internal static class PatchSourceV4StrictNewtabScratchConfigV1
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\rebuild-walkthrough-v4.cs";
        public const string Find_01 = "var T = Line.Trim(); if (!T.Contains(\"chrome://newtab/\", StringComparison.Ordinal)) continue; var Colon = T.IndexOf(':'); if (Colon < 1) continue; var Idx = T.Substring(0, Colon).Trim(); if (Idx.All(char.IsDigit)) { NtIdx = Idx; break; }";
        public const string Replace_01 = "var T = Line.Trim(); var ColonSpace = T.IndexOf(\": \", StringComparison.Ordinal); if (ColonSpace < 1) continue; var Idx = T.Substring(0, ColonSpace).Trim(); if (!Idx.All(char.IsDigit)) continue; var Rest = T.Substring(ColonSpace + 2); var SpaceParen = Rest.IndexOf(\" (\", StringComparison.Ordinal); var TabUrl = SpaceParen > 0 ? Rest.Substring(0, SpaceParen).TrimEnd() : Rest.TrimEnd(); if (!TabUrl.StartsWith(\"chrome://newtab/\", StringComparison.Ordinal)) continue; NtIdx = Idx; break;";
    }
}
