#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs

// stage-amend-push.cs - explicitly stage the new helper scripts plus tracked
// changes, amend the single main commit, and force-push. Mirrors the
// "one commit per branch" memory rule.
using System.Diagnostics;
using Scripts;

static string Run(string Cmd)
{
    var Psi = new ProcessStartInfo("git", Cmd) { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, WorkingDirectory = Paths.Repo };
    Psi.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
    Psi.EnvironmentVariables["GIT_EDITOR"] = "true";
    using var P = Process.Start(Psi)!;
    P.StandardInput.Close();
    var Out = P.StandardOutput.ReadToEnd();
    var Err = P.StandardError.ReadToEnd();
    P.WaitForExit();
    return Out + Err;
}

var Untracked = Directory.GetFiles(System.IO.Path.Combine(Paths.Repo, "scripts"), "*.cs", SearchOption.TopDirectoryOnly)
    .Select(F => "scripts/" + System.IO.Path.GetFileName(F).Replace('\\', '/'))
    .Concat(["wwwroot/app/", "docs/", "src/", "worker/"])
    .ToArray();
foreach (var U in Untracked)
{
    var Full = System.IO.Path.Combine(Paths.Repo, U.Replace('/', System.IO.Path.DirectorySeparatorChar));
    if (File.Exists(Full) || Directory.Exists(Full))
    {
        Console.WriteLine(Run($"add -- \"{U}\""));
    }
}
Console.WriteLine(Run("add -u"));
Console.WriteLine(Run("status -s"));
Console.WriteLine(Run("commit --amend --no-edit"));
Console.WriteLine(Run("push origin main --force-with-lease"));
return 0;
