namespace WolfsTruckingCo.Scripts.Specific;

public static class PatchSourceScratchConfigVChatScrollScss
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss\app.scss";
    public const string Find_01 = ".ChatStream { display: flex; flex-direction: column; gap: 10px; margin-bottom: 14px; }";
    public const string Replace_01 = ".ChatStream { display: flex; flex-direction: column; gap: 10px; margin-bottom: 14px; min-height: 40vh; max-height: 60vh; overflow-y: auto; overflow-anchor: auto; padding: 12px; scroll-behavior: smooth; }\n.ChatStream > * { overflow-anchor: none; }\n.ChatStream > *:last-child { overflow-anchor: auto; scroll-margin-bottom: 12px; }";
    public const string Find_02 = ".ChatInputRow {";
    public const string Replace_02 = ".ChatInputRow { position: sticky; bottom: 0; background: var(--bg); padding: 8px 0; z-index: 5; ";
}
