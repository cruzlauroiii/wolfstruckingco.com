return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\rebuild-walkthrough-v3.cs";
        public const string Find_01 = "async Task ClearStorageAndReload()";
        public const string Replace_01 = "async Task FillMicrosoftEmailNoSubmit(string Account)\n{\n    var Fn = \"() => { var h = location.host; if (h.indexOf('login.microsoftonline.com') === -1 && h.indexOf('login.live.com') === -1) return 'not-ms'; var i = document.querySelector('input[type=email],input[name=loginfmt]'); if (!i) return 'no-input'; var nv = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype,'value').set; nv.call(i, '\" + Account + \"'); i.dispatchEvent(new Event('input',{bubbles:true})); i.dispatchEvent(new Event('change',{bubbles:true})); return 'filled'; }\";\n    await Cdp(\"msfill-nosubmit\", Eval(Fn));\n}\n\nasync Task ClearStorageAndReload()";
        public const string Find_02 = "        if (Pad == \"001\")";
        public const string Replace_02 = "        if (Pad == \"051\")\n        {\n            await ClickLogoutFirst();\n            await Task.Delay(3000);\n            await ClickSsoButton(\"microsoft\");\n            await Task.Delay(8000);\n            await FillMicrosoftEmailNoSubmit(SsoAccount);\n            await Task.Delay(2000);\n        }\n        if (Pad == \"001\")";
    }
}
