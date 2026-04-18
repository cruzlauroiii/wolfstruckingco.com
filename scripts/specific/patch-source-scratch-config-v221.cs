namespace Scripts;

internal static class PatchSourceScratchConfigV221
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\wipe-db.cs";

    public const string Find_01 = "var WipeBody = await WipeResp.Content.ReadAsStringAsync();\nConsole.WriteLine($\"wipe: {(int)WipeResp.StatusCode} {WipeResp.ReasonPhrase} {WipeBody}\");\nif (!WipeResp.IsSuccessStatusCode) { return 2; }";
    public const string Replace_01 = "if (!WipeResp.IsSuccessStatusCode) { return 2; }";

    public const string Find_02 = "var VerifyBody = await VerifyResp.Content.ReadAsStringAsync();\nConsole.WriteLine($\"verify: {(int)VerifyResp.StatusCode} {VerifyResp.ReasonPhrase} {VerifyBody}\");\nif (!VerifyResp.IsSuccessStatusCode) { return 4; }";
    public const string Replace_02 = "var VerifyBody = await VerifyResp.Content.ReadAsStringAsync();\nif (!VerifyResp.IsSuccessStatusCode) { return 4; }";

    public const string Find_03 = "    if (Count == 0 && Items == 0) { Console.WriteLine(\"verify ok: listings empty\"); return 0; }";
    public const string Replace_03 = "    if (Count == 0 && Items == 0) { return 0; }";
}
