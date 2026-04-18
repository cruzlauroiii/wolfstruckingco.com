return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV79
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";

        public const string Find_01 = "    var MarketScrollExpr = \"(function(){try{if(location.pathname.indexOf('/Marketplace')>-1){var l=document.querySelector('.Listing,.MarketplaceCard,article.Card');if(l){l.scrollIntoView({block:'center'});}}}catch(e){}})()\";\n    await Cdp.SendAsync(\"Runtime.evaluate\", new { expression = MarketScrollExpr, returnByValue = true });\n    await Task.Delay(200);\n    var Shot = await Cdp.SendOnceAsync";
        public const string Replace_01 = "    var MarketScrollExpr = \"(function(){try{if(location.pathname.indexOf('/Marketplace')>-1){var l=document.querySelector('.Listing,.MarketplaceCard,article.Card');if(l){l.scrollIntoView({block:'center'});}}}catch(e){}})()\";\n    await Cdp.SendAsync(\"Runtime.evaluate\", new { expression = MarketScrollExpr, returnByValue = true });\n    await Task.Delay(200);\n    var DocsScrollExpr = \"(function(){try{if(location.pathname.indexOf('/Documents')>-1){var d=document.querySelector('.DocBadge.Done')||document.querySelector('.DocBadge,.DocCard,.UploadedDoc');if(d){d.scrollIntoView({block:'center'});}}}catch(e){}})()\";\n    await Cdp.SendAsync(\"Runtime.evaluate\", new { expression = DocsScrollExpr, returnByValue = true });\n    await Task.Delay(200);\n    var Shot = await Cdp.SendOnceAsync";

        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
