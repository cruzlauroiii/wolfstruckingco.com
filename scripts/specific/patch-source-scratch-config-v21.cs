return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV21
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Services\WolfsJsBootstrap.cs";
        public const string Find_01 = "        \"      var m = location.search.match(/[?&]sso=([a-z]+)/i);\" + \"\\n\" +\n        \"      if (m) { w.ssoLogin(m[1]); }\" + \"\\n\" +";
        public const string Replace_01 = "        \"      var m = location.search.match(/[?&]sso=([a-z]+)/i);\" + \"\\n\" +\n        \"      if (m) { w.ssoLogin(m[1]); }\" + \"\\n\" +\n        \"      var sm = location.search.match(/[?&]signout=1/i);\" + \"\\n\" +\n        \"      if (sm) { w.authClear(); var soBase = location.pathname.replace(/Settings\\\\/?$/, ''); location.replace(soBase); }\" + \"\\n\" +";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
