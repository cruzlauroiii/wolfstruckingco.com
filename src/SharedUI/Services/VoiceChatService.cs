using Microsoft.JSInterop;

#pragma warning disable IDE0130

namespace SharedUI.Services;

public sealed class VoiceChatService(IJSRuntime Js)
{
    private const string Start = "WolfsChatVoice.start";
    private const string Stop = "WolfsChatVoice.stop";

    public ValueTask<bool> StartCallAsync() => Js.InvokeAsync<bool>(Start);

    public ValueTask<string> StopCallAsync() => Js.InvokeAsync<string>(Stop);
}
