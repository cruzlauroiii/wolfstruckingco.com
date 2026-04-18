return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ChatBox.razor";

        public const string Find_01 = "<div class=\"ChatInputRow\">\n    <input type=\"text\" @bind=\"Draft\" @bind:event=\"oninput\" @onkeydown=\"OnKeyAsync\" placeholder=\"@Placeholder\" />\n    <button class=\"Btn Ghost ChatBtnRound\" type=\"button\" title=\"@CallTitle\" @onclick=\"ToggleCallAsync\">@(InCall ? \"📵\" : \"📞\")</button>\n    <button class=\"Btn Ghost ChatBtnRound\" type=\"button\" title=\"@AttachTitle\" @onclick=\"AttachAsync\">📎</button>\n    <button class=\"Btn ChatBtnRound Send\" type=\"button\" title=\"Send\" disabled=\"@Sending\" @onclick=\"SendAsync\">@(Sending ? \"⏳\" : \"➤\")</button>\n</div>";

        public const string Replace_01 = "<form class=\"ChatInputRow\" method=\"get\" action=\"\">\n    <input type=\"text\" name=\"msg\" value=\"@Draft\" placeholder=\"@Placeholder\" />\n    <button class=\"Btn Ghost ChatBtnRound\" type=\"button\" title=\"@CallTitle\" @onclick=\"ToggleCallAsync\">@(InCall ? \"📵\" : \"📞\")</button>\n    <button class=\"Btn Ghost ChatBtnRound\" type=\"button\" title=\"@AttachTitle\" @onclick=\"AttachAsync\">📎</button>\n    <button class=\"Btn ChatBtnRound Send\" type=\"submit\" title=\"Send\">➤</button>\n</form>";
    }
}
