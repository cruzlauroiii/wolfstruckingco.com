return 0;

namespace Scripts
{
    internal static class RunWithWatchdogPipelineConfig
    {
        public const string TargetCs = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";
        public const string WorkingDir = @"C:\repo\public\wolfstruckingco.com\main";
        public const string ErrorPattern = @"\b(error|exception|TimeoutException|InvalidOperationException|HttpRequestException|site\s+can(?:'|n)?t\s+be\s+reached|ERR_CONNECTION_REFUSED|ERR_NAME_NOT_RESOLVED|ERR_INTERNET_DISCONNECTED|404\s+not\s+found|502\s+bad\s+gateway|navigation\s+failed|build\s+failed|fail-fast)\b";
        public const int StallSec = 60;
    }
}
