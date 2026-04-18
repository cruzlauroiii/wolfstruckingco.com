return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ChatBox.razor";
        public const string Find_01 = "    <button class=\"Btn Ghost ChatBtnRound\" type=\"button\" title=\"@CallTitle\" @onclick=\"ToggleCallAsync\">@(InCall ? \"📵\" : \"📞\")</button>";
        public const string Replace_01 = "    <a class=\"Btn Ghost ChatBtnRound\" href=\"tel:+15555550100\" title=\"@CallTitle\" role=\"button\">📞</a>";
        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
