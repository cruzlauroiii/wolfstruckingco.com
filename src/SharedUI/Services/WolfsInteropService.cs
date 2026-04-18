using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace SharedUI.Services;

public sealed class WolfsInteropService(IJSRuntime Js, AuthenticationStateProvider AuthState)
{
    private const string MapInit = "WolfsInterop.mapInit";
    private const string MapSetView = "WolfsInterop.mapSetView";
    private const string MapPanBy = "WolfsInterop.mapPanBy";
    private const string MapDestroy = "WolfsInterop.mapDestroy";
    private const string MapAddPin = "WolfsInterop.mapAddPin";
    private const string DbAllJson = "WolfsInterop.dbAllJson";
    private const string DbPut = "WolfsInterop.dbPut";
    private const string DbGet = "WolfsInterop.dbGet";
    private const string WorkerPost = "WolfsInterop.workerPost";
    private const string WorkerGet = "WolfsInterop.workerGet";
    private const string ThemeRead = "WolfsInterop.themeRead";
    private const string ThemeWrite = "WolfsInterop.themeWrite";
    private const string ThemeResolved = "WolfsInterop.themeResolved";
    private const string AuthGet = "WolfsInterop.authGet";
    private const string AuthSet = "WolfsInterop.authSet";
    private const string AuthClear = "WolfsInterop.authClear";

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ValueTask<bool> MapInitAsync(string ElementId, double Lat, double Lng, int Zoom, string Theme)
        => Js.InvokeAsync<bool>(MapInit, ElementId, Lat, Lng, Zoom, Theme);
    public ValueTask MapSetViewAsync(string ElementId, double Lat, double Lng, int Zoom)
        => Js.InvokeVoidAsync(MapSetView, ElementId, Lat, Lng, Zoom);
    public ValueTask MapPanByAsync(string ElementId, double Dx, double Dy)
        => Js.InvokeVoidAsync(MapPanBy, ElementId, Dx, Dy);
    public ValueTask MapDestroyAsync(string ElementId)
        => Js.InvokeVoidAsync(MapDestroy, ElementId);
    public ValueTask MapAddPinAsync(string ElementId, double Lat, double Lng, string Color, string Label)
        => Js.InvokeVoidAsync(MapAddPin, ElementId, Lat, Lng, Color, Label);

    public async ValueTask<List<T>> DbAllAsync<T>(string Store)
    {
        var Json = await Js.InvokeAsync<string>(DbAllJson, Store);
        var Result = JsonSerializer.Deserialize<List<T>>(Json, JsonOpts);
        return Result ?? [];
    }
    public ValueTask DbPutAsync<T>(string Store, T Value)
        => Js.InvokeVoidAsync(DbPut, Store, Value);
    public ValueTask<T?> DbGetAsync<T>(string Store, string Id)
        => Js.InvokeAsync<T?>(DbGet, Store, Id);

    public ValueTask<WorkerResponse> WorkerPostAsync(string Path, object Body, Dictionary<string, string>? Headers = null)
        => Js.InvokeAsync<WorkerResponse>(WorkerPost, Path, Body, Headers ?? []);
    public ValueTask<WorkerResponse> WorkerGetAsync(string Path)
        => Js.InvokeAsync<WorkerResponse>(WorkerGet, Path);

    public ValueTask<string> ThemeReadAsync() => Js.InvokeAsync<string>(ThemeRead);
    public ValueTask ThemeWriteAsync(string Theme) => Js.InvokeVoidAsync(ThemeWrite, Theme);
    public ValueTask<string> ThemeResolvedAsync() => Js.InvokeAsync<string>(ThemeResolved);

    public ValueTask<AuthState> AuthGetAsync() => Js.InvokeAsync<AuthState>(AuthGet);
    public async ValueTask AuthSetAsync(string? Role, string? Email, string? Session)
    {
        await Js.InvokeVoidAsync(AuthSet, Role, Email, Session);
        if (AuthState is LocalStorageAuthStateProvider Provider) { Provider.NotifyChanged(); }
    }
    public async ValueTask AuthClearAsync()
    {
        await Js.InvokeVoidAsync(AuthClear);
        if (AuthState is LocalStorageAuthStateProvider Provider) { Provider.NotifyChanged(); }
    }

    private const string SetMapHeight = "WolfsInterop.setMapHeight";

    public ValueTask SetMapHeightAsync(string ElementId, int Height)
        => Js.InvokeVoidAsync(SetMapHeight, ElementId, Height);

    private const string ChatReply = "WolfsInterop.chatReply";
    private const string StartCall = "WolfsInterop.startCall";
    private const string EndCall = "WolfsInterop.endCall";
    private const string Recognize = "WolfsInterop.recognize";
    private const string Speak = "WolfsInterop.speak";

    public ValueTask<RecognizeResult> RecognizeAsync() => Js.InvokeAsync<RecognizeResult>(Recognize);

    private const string RecognizeStop = "WolfsInterop.recognizeStop";
    private const string ScrollChatBottom = "WolfsInterop.scrollChatBottom";

    public ValueTask SpeakAsync(string Text, string Voice = "") => Js.InvokeVoidAsync(Speak, Text, Voice);
    public ValueTask RecognizeStopAsync() => Js.InvokeVoidAsync(RecognizeStop);
    public ValueTask ScrollChatBottomAsync() => Js.InvokeVoidAsync(ScrollChatBottom);
    private const string ThemeCycle = "WolfsInterop.themeCycle";
    private const string SsoLogin = "WolfsInterop.ssoLogin";

    public ValueTask<string> ChatReplyAsync(string System, IReadOnlyList<ChatMessage> History, int MaxTokens)
        => Js.InvokeAsync<string>(ChatReply, System, History, MaxTokens);

    public ValueTask<string> StartCallAsync(string Role, string Subject)
        => Js.InvokeAsync<string>(StartCall, Role, Subject);

    public ValueTask EndCallAsync() => Js.InvokeVoidAsync(EndCall);

    public ValueTask<string> ThemeCycleAsync() => Js.InvokeAsync<string>(ThemeCycle);

    public ValueTask SsoLoginAsync(string Provider) => Js.InvokeVoidAsync(SsoLogin, Provider);
}

public record ChatMessage(string Role, string Content);

public record WorkerResponse(bool Ok, int Status, string Body);
public record AuthState(string? Role, string? Email, string? Sess);

public sealed record RecognizeResult(string? Text, string? Error);
