namespace Scripts;

internal static class PatchSourceScratchConfigV224
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";

    public const string Find_01 = "Console.WriteLine($\"reset scene-effect stores; db = {File.ReadAllLines(DbPath).Length} base rows\");";
    public const string Replace_01 = "_ = File.ReadAllLines(DbPath).Length;";

    public const string Find_02 = "    if (!string.IsNullOrEmpty(WipeOut)) { Console.Write(WipeOut); }\n";
    public const string Replace_02 = "";

    public const string Find_03 = "Console.WriteLine($\"loaded {Scenes.Length} scenes\");";
    public const string Replace_03 = "";

    public const string Find_04 = "    Console.WriteLine($\"  ai: {CacheKey} → {Text[..Math.Min(60, Text.Length)]}...\");";
    public const string Replace_04 = "";

    public const string Find_05 = "Console.WriteLine($\"AI chat: {SceneChat.Count} scenes generated, {AiCache.Count} entries in cache\");";
    public const string Replace_05 = "";

    public const string Find_06 = "    Console.WriteLine($\"  → scene {Pad} {Route} navigating\");";
    public const string Replace_06 = "";

    public const string Find_07 = "            Console.WriteLine($\"    ! pnf gate: {Pad} {Route} attempt {Attempt}/3 → retry\");";
    public const string Replace_07 = "";

    public const string Find_08 = "            Console.WriteLine($\"  ✓ {Pad} {Route}\");";
    public const string Replace_08 = "";

    public const string Find_09 = "            Console.WriteLine($\"    ! sso warn: localStorage not populated for {SsoProvider} ({SsoSession})\");";
    public const string Replace_09 = "";

    public const string Find_10 = "            Console.WriteLine($\"    sso: {SsoProvider} → {ResolvedActor} (session={SsoSession[..Math.Min(28, SsoSession.Length)]}...)\");";
    public const string Replace_10 = "";

    public const string Find_11 = "Console.WriteLine($\"\\nOCR uniqueness: {Distinct}/{TotalScenes}\");";
    public const string Replace_11 = "";

    public const string Find_12 = "Console.WriteLine($\"wrote frame-references.md\");";
    public const string Replace_12 = "";
}
