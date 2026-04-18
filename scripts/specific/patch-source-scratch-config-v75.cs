return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV75
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";

        public const string Find_01 = "    var ChatScrollExpr = \"(function(){try{if(location.pathname.indexOf('/Chat')>-1){var s=document.querySelector('.ChatStream');if(s&&s.lastElementChild){s.lastElementChild.scrollIntoView({block:'center'});}}}catch(e){}})()\";\n    await Cdp.SendAsync(\"Runtime.evaluate\", new { expression = ChatScrollExpr, returnByValue = true });\n    await Task.Delay(200);\n    var ScreenShotCss";
        public const string Replace_01 = "    var ChatScrollExpr = \"(function(){try{if(location.pathname.indexOf('/Chat')>-1){var s=document.querySelector('.ChatStream');if(s&&s.lastElementChild){s.lastElementChild.scrollIntoView({block:'center'});}}}catch(e){}})()\";\n    await Cdp.SendAsync(\"Runtime.evaluate\", new { expression = ChatScrollExpr, returnByValue = true });\n    await Task.Delay(200);\n    var MarketScrollExpr = \"(function(){try{if(location.pathname.indexOf('/Marketplace')>-1){var l=document.querySelector('.Listing,.MarketplaceCard,article.Card');if(l){l.scrollIntoView({block:'center'});}}}catch(e){}})()\";\n    await Cdp.SendAsync(\"Runtime.evaluate\", new { expression = MarketScrollExpr, returnByValue = true });\n    await Task.Delay(200);\n    var ScreenShotCss";

        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
