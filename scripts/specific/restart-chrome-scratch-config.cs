return 0;

namespace Scripts
{
    internal static class RestartChromeScratchConfig
    {
        public const string ProcessName = "chrome";
        public const string LaunchPath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
        public const string FallbackPath = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
        public const string WaitMs = "8000";
    }
}
