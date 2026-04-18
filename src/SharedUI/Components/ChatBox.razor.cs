using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using SharedUI.Services;

namespace SharedUI.Components;

public partial class ChatBox
{
    private const string RoleAssistant = "assistant";
    private const string RoleUser = "user";
    private const string RoleAgent = "agent";
    private const int MaxTokens = 256;
    private const string EnterKey = "Enter";
    private const string DefaultSystemPrompt = "You are Wolfs Trucking dispatcher. Reply briefly.";
    private const string DefaultPlaceholder = "Type your reply…";
    private const string DefaultAssistantLabel = "Agent";
    private const string DefaultUserLabel = "You";
    private const string DefaultCallTitle = "Call agent";
    private const string DefaultAttachTitle = "Attach file";

    private const char EmDash = '—';

    private const char EnDash = '–';

    private const char SpaceChar = ' ';

    private const string BoldOpen = "<strong>";

    private const string BoldClose = "</strong>";

    private const string BoldReplaceTemplate = BoldOpen + "$1" + BoldClose;

    private const long MaxAttachmentBytes = 10 * 1024 * 1024;

    private const int MaxAttachmentCount = 8;

    private const string AttachedNotePrefix = "📎 attached: ";

    private const string JoinSeparator = ", ";

    private const string AssistantFallback = "(agent offline — your message was logged)";

    private const string ChatAttachUrl = Domain.Constants.WorkerConstants.ChatAttachUrl;

    private const string FilenameQueryKey = "?filename=";

    private const string DefaultContentType = "application/octet-stream";

    private const string UploadFailedSuffix = " (upload failed)";

    private const string UploadErrorSuffix = " (upload error)";

    private const string BadgeAnalysisPrompt = "You are a CDL hiring assistant for Wolfs Trucking. The user just uploaded one or more documents and the URLs are listed in the previous user message as plain text. Assume each document is a scan of a CDL, endorsement, certification, or transport credential. Reply with one short comma separated list of likely Wolfs badges that the document could represent. The Wolfs badge vocabulary is CDL-A, CDL-B, Hazmat, Tanker, Doubles, PortPass, Interstate, Auto, China-export, and Team. Write in plain natural English. Do not use markdown, asterisks, emoji, numbered placeholders, or template tokens of any kind.";

    private const string FileUrlPrefix = Domain.Constants.WorkerConstants.Origin;

    private const string UrlJsonField = "url";

    private const string UploadUrlPattern = "\"url\":\"(?<" + UrlJsonField + ">[^\"]+)\"";

    private const int UploadUrlRegexTimeoutMs = 200;

    private const string FilenameUrlSeparator = " ";

    private const string TemplateTokenPattern = "\\$\\d+";

    private const int TemplateRegexTimeoutMs = 200;

    private const int LoopIdleDelayMs = 120;

    [Parameter]
    public string SystemPrompt { get; set; } = DefaultSystemPrompt;

    [Parameter]
    public string Placeholder { get; set; } = DefaultPlaceholder;

    [Parameter]
    public string AssistantLabel { get; set; } = DefaultAssistantLabel;

    [Parameter]
    public string UserLabel { get; set; } = DefaultUserLabel;

    [Parameter]
    public string CallTitle { get; set; } = DefaultCallTitle;

    [Parameter]
    public string AttachTitle { get; set; } = DefaultAttachTitle;

    [Parameter]
    public string Subject { get; set; } = string.Empty;

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    [Inject]
    private HttpClient Http { get; set; } = null!;

    private string Draft { get; set; } = string.Empty;

    private bool Sending { get; set; }

    private bool InCall { get; set; }

    private List<ChatMessage> Live { get; } = [];

    protected override void OnInitialized()
    {
        Live.Clear();
        foreach (var Turn in WolfsRenderContext.ChatHistory)
        {
            var Role = string.Equals(Turn.Role, RoleAgent, StringComparison.OrdinalIgnoreCase) ? RoleAssistant : RoleUser;
            Live.Add(new ChatMessage(Role, Turn.Text));
        }
    }

#pragma warning disable IDE1006, WT001, S927, RCS1168
    protected override async Task OnAfterRenderAsync(bool firstRender)
#pragma warning restore IDE1006, WT001, S927, RCS1168
    {
        try
        {
            await Wolfs.ScrollChatBottomAsync();
        }
#pragma warning disable CA1031
        catch (Microsoft.JSInterop.JSException Ex)
#pragma warning restore CA1031
        {
            _ = Ex;
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex("\\*\\*([^*]+)\\*\\*", System.Text.RegularExpressions.RegexOptions.ExplicitCapture, 200)]
    private static partial System.Text.RegularExpressions.Regex BoldRe();

    [System.Text.RegularExpressions.GeneratedRegex(UploadUrlPattern, System.Text.RegularExpressions.RegexOptions.ExplicitCapture, UploadUrlRegexTimeoutMs)]
    private static partial System.Text.RegularExpressions.Regex UploadUrlRe();

    [System.Text.RegularExpressions.GeneratedRegex(TemplateTokenPattern, System.Text.RegularExpressions.RegexOptions.ExplicitCapture, TemplateRegexTimeoutMs)]
    private static partial System.Text.RegularExpressions.Regex TemplateRe();

    private static string RenderRich(string Content)
    {
        if (string.IsNullOrEmpty(Content)) { return string.Empty; }
        var Untemplated = TemplateRe().Replace(Content, string.Empty);
        var Stripped = Untemplated.Replace(EmDash, SpaceChar).Replace(EnDash, SpaceChar);
        var Encoded = System.Net.WebUtility.HtmlEncode(Stripped);
        return BoldRe().Replace(Encoded, BoldReplaceTemplate);
    }

    private async Task SendAsync()
    {
        var Msg = (Draft ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(Msg) || Sending) { return; }
        Sending = true;
        Live.Add(new ChatMessage(RoleUser, Msg));
        Draft = string.Empty;
        try
        {
            var Reply = await Wolfs.ChatReplyAsync(SystemPrompt, Live, MaxTokens);
            Live.Add(new ChatMessage(RoleAssistant, string.IsNullOrEmpty(Reply) ? AssistantFallback : Reply));
        }
#pragma warning disable CA1031
        catch
#pragma warning restore CA1031
        {
            Live.Add(new ChatMessage(RoleAssistant, AssistantFallback));
        }
        finally { Sending = false; }
    }

    private async Task OnKeyAsync(KeyboardEventArgs E)
    {
        if (string.Equals(E.Key, EnterKey, StringComparison.Ordinal) && !E.ShiftKey) { await SendAsync(); }
    }

    private async Task StartCallAsync()
    {
        if (InCall)
        {
#pragma warning disable CA1031
            try { await Wolfs.RecognizeStopAsync(); } catch (Microsoft.JSInterop.JSException Ex2) { _ = Ex2; }
#pragma warning restore CA1031
            InCall = false;
            StateHasChanged();
            return;
        }

        InCall = true;
        StateHasChanged();
        while (InCall)
        {
            try
            {
                var Rec = await Wolfs.RecognizeAsync();
                if (!InCall) { break; }
                if (!string.IsNullOrEmpty(Rec.Text))
                {
                    Draft = Rec.Text;
                    await SendAsync();
                    if (Live.Count > 0 && string.Equals(Live[^1].Role, RoleAssistant, StringComparison.Ordinal))
                    {
                        await Wolfs.SpeakAsync(Live[^1].Content, string.Empty);
                    }
                }
                else
                {
                    await Task.Delay(LoopIdleDelayMs);
                }
            }
#pragma warning disable CA1031
            catch (Microsoft.JSInterop.JSException)
#pragma warning restore CA1031
            {
                Live.Add(new ChatMessage(RoleAssistant, AssistantFallback));
                break;
            }
        }

        InCall = false;
        StateHasChanged();
    }

    private async Task OnFilesAttachedAsync(InputFileChangeEventArgs E)
    {
        var Names = new List<string>();
        foreach (var F in E.GetMultipleFiles(maximumFileCount: MaxAttachmentCount))
        {
            if (F.Size > MaxAttachmentBytes) { continue; }
            try
            {
#pragma warning disable S5693
                await using var Stream = F.OpenReadStream(MaxAttachmentBytes);
#pragma warning restore S5693
                using var Content = new StreamContent(Stream);
                Content.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrEmpty(F.ContentType) ? DefaultContentType : F.ContentType);
                var Url = ChatAttachUrl + FilenameQueryKey + Uri.EscapeDataString(F.Name);
                using var Response = await Http.PostAsync(new Uri(Url), Content);
                if (Response.IsSuccessStatusCode)
                {
                    var Body = await Response.Content.ReadAsStringAsync();
                    var Match = UploadUrlRe().Match(Body);
                    var FileUrl = Match.Success ? FileUrlPrefix + Match.Groups[UrlJsonField].Value : string.Empty;
                    Names.Add(string.IsNullOrEmpty(FileUrl) ? F.Name : F.Name + FilenameUrlSeparator + FileUrl);
                }
                else
                {
                    Names.Add(F.Name + UploadFailedSuffix);
                }
            }
#pragma warning disable CA1031
            catch
#pragma warning restore CA1031
            {
                Names.Add(F.Name + UploadErrorSuffix);
            }
        }

        if (Names.Count > 0)
        {
            var Note = AttachedNotePrefix + string.Join(JoinSeparator, Names);
            Live.Add(new ChatMessage(RoleUser, Note));
            try
            {
                var Reply = await Wolfs.ChatReplyAsync(BadgeAnalysisPrompt, Live, MaxTokens);
                Live.Add(new ChatMessage(RoleAssistant, string.IsNullOrEmpty(Reply) ? AssistantFallback : Reply));
            }
#pragma warning disable CA1031
            catch (Microsoft.JSInterop.JSException)
#pragma warning restore CA1031
            {
                Live.Add(new ChatMessage(RoleAssistant, AssistantFallback));
            }
        }
    }
}
