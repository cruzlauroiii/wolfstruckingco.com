return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV147
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";

        public const string Find_01 = "        await Cdp.SendAsync(\"Runtime.evaluate\", new { expression = HiringScrollExpr, returnByValue = true });\n        await Task.Delay(200);\n        var Shot = await Cdp.SendOnceAsync(\"Page.captureScreenshot\", new { format = \"png\" }, 90);";
        public const string Replace_01 = "        await Cdp.SendAsync(\"Runtime.evaluate\", new { expression = HiringScrollExpr, returnByValue = true });\n        await Task.Delay(200);\n        var ChatInputRowScrollExpr = \"(function(){try{if(location.pathname.indexOf('/Chat')>-1){var ir=document.querySelector('.ChatInputRow,.ChatComposeRow,.Compose,.InputRow,.FormRow:last-of-type,.Btn:last-of-type');if(ir){ir.scrollIntoView({block:'end',inline:'nearest'});}}}catch(e){}})()\";\n        await Cdp.SendAsync(\"Runtime.evaluate\", new { expression = ChatInputRowScrollExpr, returnByValue = true });\n        await Task.Delay(200);\n        var Shot = await Cdp.SendOnceAsync(\"Page.captureScreenshot\", new { format = \"png\" }, 90);";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
