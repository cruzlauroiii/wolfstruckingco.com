return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV17
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";
        public const string Find_01 = "    await Cdp.DismissInfobarAsync();\n    var ScreenShotCss = \"";
        public const string Replace_01 = "    if (SceneChat.TryGetValue(N, out var SceneBubbles) && SceneBubbles.Count > 0)\n    {\n        var BubbleHtml = new System.Text.StringBuilder();\n        foreach (var Turn in SceneBubbles)\n        {\n            var BCls = Turn.Role == \"agent\" ? \"Agent\" : \"User\";\n            var BLbl = Turn.Role == \"agent\" ? \"Agent\" : \"You\";\n            BubbleHtml.Append(\"<div class=\\\"ChatBubble \").Append(BCls).Append(\"\\\"><strong>\").Append(BLbl).Append(\"</strong>\").Append(System.Net.WebUtility.HtmlEncode(Turn.Text)).Append(\"</div>\");\n        }\n        var BubbleJs = \"(function(){var s=document.querySelector('.ChatStream');if(s){s.innerHTML=\" + JsonSerializer.Serialize(BubbleHtml.ToString()) + \";}})()\";\n        await Cdp.SendAsync(\"Runtime.evaluate\", new { expression = BubbleJs, returnByValue = true });\n        await Task.Delay(150);\n    }\n    await Cdp.DismissInfobarAsync();\n    var ScreenShotCss = \"";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
