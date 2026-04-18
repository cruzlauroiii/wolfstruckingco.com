namespace WolfsTruckingCo.Scripts.Specific;

public static class PatchSourceScratchConfigVDeployTrim
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\deploy-worker.cs";
    public const string Find_01 = "return Code;\n\n";
    public const string Replace_01 = "return Code;\n";
}
