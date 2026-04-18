namespace WolfsTruckingCo.Scripts.Specific;

public static class PatchSourceScratchConfigVSafeLimit
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ChatBox.razor.cs";
    public const string Find_01 = "                await using var Stream = F.OpenReadStream(MaxAttachmentBytes);";
    public const string Replace_01 = "#pragma warning disable S5693\n                await using var Stream = F.OpenReadStream(MaxAttachmentBytes);\n#pragma warning restore S5693";
}
