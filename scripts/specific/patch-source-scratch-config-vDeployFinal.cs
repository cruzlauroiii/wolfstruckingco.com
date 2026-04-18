namespace WolfsTruckingCo.Scripts.Specific;

public static class PatchSourceScratchConfigVDeployFinal
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\deploy-worker.cs";
    public const string Find_01 = "}\n\ntry { /* keep worker.js as source of truth */ }\ncatch (IOException Ex) { await Console.Error.WriteLineAsync($\"cleanup: {Ex.Message}\"); }\nreturn Code;\n";
    public const string Replace_01 = "}\nreturn Code;\n";
}
