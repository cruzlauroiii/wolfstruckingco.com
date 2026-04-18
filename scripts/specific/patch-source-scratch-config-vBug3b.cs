return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigVBug3B
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ChatBox.razor";

        public const string Find_01 = "<div class=\"ChatInputRow\">\n    <input type=\"text\" @bind=\"Draft\" @bind:event=\"oninput\" @onkeydown=\"OnKeyAsync\" placeholder=\"@Placeholder\" />";
        public const string Replace_01 = "<form class=\"ChatInputRow\" action=\"/Chat/\" method=\"get\">\n    <input type=\"text\" name=\"msg\" @bind=\"Draft\" @bind:event=\"oninput\" @onkeydown=\"OnKeyAsync\" placeholder=\"@Placeholder\" />";

        public const string Find_02 = "    <button class=\"Btn ChatBtnRound Send\" type=\"button\" title=\"Send\" disabled=\"@Sending\" @onclick=\"SendAsync\">@(Sending ? \"⏳\" : \"➤\")</button>\n</div>";
        public const string Replace_02 = "    <button class=\"Btn ChatBtnRound Send\" type=\"submit\" title=\"Send\" disabled=\"@Sending\" @onclick=\"SendAsync\">@(Sending ? \"⏳\" : \"➤\")</button>\n</form>";
    }
}
