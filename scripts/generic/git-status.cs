#:property TargetFramework=net11.0

// git-status.cs — print short status + last 3 commits + remote sync state.
using System.Diagnostics;

static async Task<string> Run(string Cmd)
{
    var Psi = new ProcessStartInfo("git", Cmd) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, WorkingDirectory = @"C:\repo\public\wolfstruckingco.com\main" };
    using var P = Process.Start(Psi)!;
    await P.WaitForExitAsync();
    return await P.StandardOutput.ReadToEndAsync() + await P.StandardError.ReadToEndAsync();
}
[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303", Justification = "Section header literal")]
static async Task PrintHeader(string Header) => await Console.Out.WriteLineAsync(Header);
await PrintHeader("--- status ---");
await Console.Out.WriteLineAsync(await Run("status -sb"));
await PrintHeader("--- log -3 ---");
await Console.Out.WriteLineAsync(await Run("log --oneline -3"));
await PrintHeader("--- origin/main vs HEAD ---");
await Console.Out.WriteLineAsync(await Run("log --oneline origin/main..HEAD"));
return 0;
