return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigVBug3A
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ChatBox.razor";

        public const string Find_01 = "<div class=\"ChatInputRow\">\r\n    <input type=\"text\" @bind=\"Draft\" @bind:event=\"oninput\" @onkeydown=\"OnKeyAsync\" placeholder=\"@Placeholder\" />\r\n    <button class=\"Btn Ghost ChatBtnRound\" type=\"button\" title=\"@CallTitle\" @onclick=\"ToggleCallAsync\">@(InCall ? \"\\uD83D\\uDCF5\" : \"\\uD83D\\uDCDE\")</button>\r\n    <label class=\"Btn Ghost ChatBtnRound\" title=\"@AttachTitle\" for=\"ChatAttachInput\">\\uD83D\\uDCCE</label>\r\n    <InputFile id=\"ChatAttachInput\" OnChange=\"OnFilesAttachedAsync\" class=\"HiddenInput\" />\r\n    <button class=\"Btn ChatBtnRound Send\" type=\"button\" title=\"Send\" disabled=\"@Sending\" @onclick=\"SendAsync\">@(Sending ? \"\\u23F3\" : \"\\u27A4\")</button>\r\n</div>";

        public const string Replace_01 = "<form class=\"ChatInputRow\" action=\"/Chat/\" method=\"get\">\r\n    <input type=\"text\" name=\"msg\" @bind=\"Draft\" @bind:event=\"oninput\" @onkeydown=\"OnKeyAsync\" placeholder=\"@Placeholder\" />\r\n    <button class=\"Btn Ghost ChatBtnRound\" type=\"button\" title=\"@CallTitle\" @onclick=\"ToggleCallAsync\">@(InCall ? \"\\uD83D\\uDCF5\" : \"\\uD83D\\uDCDE\")</button>\r\n    <label class=\"Btn Ghost ChatBtnRound\" title=\"@AttachTitle\" for=\"ChatAttachInput\">\\uD83D\\uDCCE</label>\r\n    <InputFile id=\"ChatAttachInput\" OnChange=\"OnFilesAttachedAsync\" class=\"HiddenInput\" />\r\n    <button class=\"Btn ChatBtnRound Send\" type=\"submit\" title=\"Send\" disabled=\"@Sending\" @onclick=\"SendAsync\">@(Sending ? \"\\u23F3\" : \"\\u27A4\")</button>\r\n</form>";
    }
}
