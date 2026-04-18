namespace Scripts;

internal static class PatchSourceScratchConfigV220
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\patch-html-signout.cs";

    public const string Find_01 = "Console.WriteLine($\"patched {Patched.ToString(System.Globalization.CultureInfo.InvariantCulture)} html files\");\nreturn 0;";
    public const string Replace_01 = "return 0;";
}
