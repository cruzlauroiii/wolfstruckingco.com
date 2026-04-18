namespace WolfsTruckingCo.Scripts.Specific;

public static class PatchSourceScratchConfigVAutostartRevert
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\generate-statics.cs";
    public const string Find_01 = "<script src=\\\"/wolfstruckingco.com/app/_framework/blazor.webassembly.js\\\" autostart=\\\"true\\\"></script>";
    public const string Replace_01 = "<script src=\\\"/wolfstruckingco.com/app/_framework/blazor.webassembly.js\\\" autostart=\\\"false\\\"></script>";
}
