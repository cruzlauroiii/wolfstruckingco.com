return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV56
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";

        public const string Find_01 = "    await Cdp.DismissInfobarAsync();\n    var ScreenShotCss";
        public const string Replace_01 = "    await Cdp.DismissInfobarAsync();\n    var ChatScrollExpr = \"(function(){try{if(location.pathname.indexOf('/Chat')>-1){var s=document.querySelector('.ChatStream');if(s&&s.lastElementChild){s.lastElementChild.scrollIntoView({block:'center'});}}}catch(e){}})()\";\n    await Cdp.SendAsync(\"Runtime.evaluate\", new { expression = ChatScrollExpr, returnByValue = true });\n    await Task.Delay(200);\n    var ScreenShotCss";

        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
