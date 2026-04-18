return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV105
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";

        public const string Find_01 = "        var ChatScrollExpr = \"(function(){try{if(location.pathname.indexOf('/Chat')>-1||location.pathname.indexOf('/Sell/Chat')>-1||location.pathname.indexOf('/Dispatcher')>-1||location.pathname.indexOf('/Applicant')>-1){var s=document.querySelector('.ChatStream');if(s){var bubbles=s.querySelectorAll('.ChatBubble');var last=bubbles.length>0?bubbles[bubbles.length-1]:s.lastElementChild;if(last){last.scrollIntoView({block:'center',inline:'nearest'});}}}}catch(e){}})()\";\n        await Cdp.SendAsync(\"Runtime.evaluate\", new { expression = ChatScrollExpr, returnByValue = true });\n        await Task.Delay(200);\n        var Shot = await Cdp.SendOnceAsync(\"Page.captureScreenshot\", new { format = \"png\" }, 90);";
        public const string Replace_01 = "        var ChatScrollExpr = \"(function(){try{if(location.pathname.indexOf('/Chat')>-1||location.pathname.indexOf('/Sell/Chat')>-1||location.pathname.indexOf('/Dispatcher')>-1||location.pathname.indexOf('/Applicant')>-1){var s=document.querySelector('.ChatStream');if(s){var bubbles=s.querySelectorAll('.ChatBubble');var last=bubbles.length>0?bubbles[bubbles.length-1]:s.lastElementChild;if(last){last.scrollIntoView({block:'center',inline:'nearest'});}}}}catch(e){}})()\";\n        await Cdp.SendAsync(\"Runtime.evaluate\", new { expression = ChatScrollExpr, returnByValue = true });\n        await Task.Delay(200);\n        var HiringScrollExpr = \"(function(){try{if(location.pathname.indexOf('/HiringHall')>-1||location.pathname.indexOf('/Admin')>-1){var rows=document.querySelectorAll('.ApplicantRow');var last=rows.length>0?rows[rows.length-1]:document.querySelector('.HiringRow:last-of-type')||document.querySelector('.ApprovedBadge:last-of-type');if(last){last.scrollIntoView({block:'end',inline:'nearest'});}}}catch(e){}})()\";\n        await Cdp.SendAsync(\"Runtime.evaluate\", new { expression = HiringScrollExpr, returnByValue = true });\n        await Task.Delay(200);\n        var Shot = await Cdp.SendOnceAsync(\"Page.captureScreenshot\", new { format = \"png\" }, 90);";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
