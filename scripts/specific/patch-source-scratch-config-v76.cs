return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV76
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";

        public const string Find_01 = "    var MarketScrollExpr = \"(function(){try{if(location.pathname.indexOf('/Marketplace')>-1){var l=document.querySelector('.Listing,.MarketplaceCard,article.Card');if(l){l.scrollIntoView({block:'center'});}}}catch(e){}})()\";\n    await Cdp.SendAsync(\"Runtime.evaluate\", new { expression = MarketScrollExpr, returnByValue = true });\n    await Task.Delay(200);\n    var ScreenShotCss = \"(function(){try{var s=document.createElement('style');s.textContent='html,body{margin:0 !important;padding:0 !important;padding-bottom:60vh !important}body>*:first-child{margin-top:0 !important;padding-top:0 !important}';document.head.appendChild(s);window.scrollTo(0,0);}catch(e){}})()\";\n    await Cdp.SendAsync(\"Runtime.evaluate\", new { expression = ScreenShotCss, returnByValue = true });\n    await Task.Delay(200);\n    var Shot = await Cdp.SendOnceAsync(\"Page.captureScreenshot\", new { format = \"png\" }, 90);";
        public const string Replace_01 = "    var ScreenShotCss = \"(function(){try{var s=document.createElement('style');s.textContent='html,body{margin:0 !important;padding:0 !important;padding-bottom:60vh !important}body>*:first-child{margin-top:0 !important;padding-top:0 !important}';document.head.appendChild(s);window.scrollTo(0,0);}catch(e){}})()\";\n    await Cdp.SendAsync(\"Runtime.evaluate\", new { expression = ScreenShotCss, returnByValue = true });\n    await Task.Delay(200);\n    var MarketScrollExpr = \"(function(){try{if(location.pathname.indexOf('/Marketplace')>-1){var l=document.querySelector('.Listing,.MarketplaceCard,article.Card');if(l){l.scrollIntoView({block:'center'});}}}catch(e){}})()\";\n    await Cdp.SendAsync(\"Runtime.evaluate\", new { expression = MarketScrollExpr, returnByValue = true });\n    await Task.Delay(200);\n    var Shot = await Cdp.SendOnceAsync(\"Page.captureScreenshot\", new { format = \"png\" }, 90);";

        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
