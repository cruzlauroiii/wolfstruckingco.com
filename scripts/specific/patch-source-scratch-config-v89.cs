return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV89
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";

        public const string Find_01 = "    var ChatScrollExpr = \"(function(){try{if(location.pathname.indexOf('/Chat')>-1||location.pathname.indexOf('/Sell/Chat')>-1||location.pathname.indexOf('/Dispatcher')>-1||location.pathname.indexOf('/Applicant')>-1){var s=document.querySelector('.ChatStream');if(s){var bubbles=s.querySelectorAll('.ChatBubble');var last=bubbles.length>0?bubbles[bubbles.length-1]:s.lastElementChild;if(last){last.scrollIntoView({block:'center',inline:'nearest'});}}}}catch(e){}})()\";";
        public const string Replace_01 = "    var ChatScrollExpr = \"(function(){try{if(location.pathname.indexOf('/Chat')>-1||location.pathname.indexOf('/Sell')>-1||location.pathname.indexOf('/Dispatcher')>-1||location.pathname.indexOf('/Applicant')>-1){var s=document.querySelector('.ChatStream,.ChatList,.MessageList');if(s){var bubbles=s.querySelectorAll('.ChatBubble');var last=bubbles.length>0?bubbles[bubbles.length-1]:s.lastElementChild;if(last){last.scrollIntoView({block:'center',inline:'nearest'});}}}}catch(e){}})()\";";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
