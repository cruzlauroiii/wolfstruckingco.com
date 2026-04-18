#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs

// git-commit-push.cs — amend the single commit on main and force-push.
// Usage:
//   dotnet run scripts/git-commit-push.cs                      # add tracked + amend + push
//   dotnet run scripts/git-commit-push.cs -- "msg subject"     # same but new subject
//   dotnet run scripts/git-commit-push.cs -- --new "msg"       # new commit (no amend)
// Always staged: only `git add -u` (tracked changes). Never adds untracked secrets.
using System.Diagnostics;
using Scripts;

static async Task<int> Run(string Cmd)
{
    var Psi = new ProcessStartInfo("git", Cmd) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, WorkingDirectory = Paths.Repo };
    Psi.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
    Psi.EnvironmentVariables["GCM_INTERACTIVE"] = "never";
    using var P = Process.Start(Psi)!;
    var ReadOut = P.StandardOutput.ReadToEndAsync();
    var ReadErr = P.StandardError.ReadToEndAsync();
    await P.WaitForExitAsync();
    var Out = await ReadOut;
    var Err = await ReadErr;
    if (P.ExitCode != 0)
    {
        await Console.Error.WriteAsync(Err);
        await Console.Error.WriteAsync(Out);
    }
    return P.ExitCode;
}

var FilteredArgs = args.Where(A => !A.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToArray();
var NewCommit = FilteredArgs.Contains("--new");
var Subject = FilteredArgs.FirstOrDefault(A => !A.StartsWith("--", StringComparison.Ordinal));

var AddUExit = await Run("add -u");
var AddPublishExit = AddUExit == 0 ? await Run("add docs/app/") : -1;
var AddExit = AddPublishExit == 0 ? AddUExit : (AddPublishExit == -1 ? AddUExit : AddPublishExit);
var CommitCmd = NewCommit ? $"commit -m \"{Subject ?? "update"}\"" : Subject is not null ? $"commit --amend -m \"{Subject}\"" : "commit --amend --no-edit";
var CommitExit = AddExit == 0 ? await Run(CommitCmd) : -1;
var PushExit = CommitExit == 0 ? await Run("push origin main --force-with-lease") : -1;
return AddExit != 0 ? 2 : CommitExit != 0 ? 3 : PushExit != 0 ? 4 : 0;
