namespace Scripts;

internal static class PatchSourceScratchConfigV215
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\generate-statics.cs";

    public const string Find_01 = "var Css = File.Exists(CssPath) ? await File.ReadAllTextAsync(CssPath).ConfigureAwait(false) : string.Empty;\nawait Console.Out.WriteLineAsync($\"inlining {Css.Length} bytes of app.css → every page is fully standalone\").ConfigureAwait(false);";
    public const string Replace_01 = "var Css = File.Exists(CssPath) ? await File.ReadAllTextAsync(CssPath).ConfigureAwait(false) : string.Empty;";

    public const string Find_02 = "await Console.Out.WriteLineAsync($\"found {Pages.Count} routable pages\").ConfigureAwait(false);\n";
    public const string Replace_02 = "";

    public const string Find_03 = "            Written++;\n            await Console.Out.WriteLineAsync($\"  ✓ {Path.GetRelativePath(Repo, OutPath)}\").ConfigureAwait(false);";
    public const string Replace_03 = "            Written++;";

    public const string Find_04 = "await Console.Out.WriteLineAsync().ConfigureAwait(false);\nawait Console.Out.WriteLineAsync(string.Create(System.Globalization.CultureInfo.InvariantCulture, $\"done — wrote {Written} static page(s), skipped {Skipped}\")).ConfigureAwait(false);\nreturn 0;";
    public const string Replace_04 = "return 0;";
}
