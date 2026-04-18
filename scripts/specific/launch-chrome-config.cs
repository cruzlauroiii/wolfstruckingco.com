return 0;

namespace Scripts
{
    internal static class LaunchChromeConfig
    {
        public const string ChromePathRel = "Google\\Chrome\\Application\\chrome.exe";
        public const string Arg1 = "--start-maximized";
        public const string Arg2 = "--remote-allow-origins=*";
        public const string Arg3 = "";
        public const int WaitMs = 6000;
    }
}
