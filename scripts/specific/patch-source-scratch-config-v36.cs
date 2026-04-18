return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV36
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\pipeline-cdp.cs";
        public const string Find_01 = "    public Task<JsonNode> SendAsync(string Method, object? Params = null) => SendOnceAsync(Method, Params, 30);";
        public const string Replace_01 = "    public Task<JsonNode> SendAsync(string Method, object? Params = null) => SendOnceAsync(Method, Params, 30);\r\n    public Task<JsonNode> SendOnceAsync(string Method, object? Params, int TimeoutSec) => SendOnceAsyncImpl(Method, Params, TimeoutSec);";
        public const string Find_02 = "    private async Task<JsonNode> SendOnceAsync(string Method, object? Params, int TimeoutSec)";
        public const string Replace_02 = "    private async Task<JsonNode> SendOnceAsyncImpl(string Method, object? Params, int TimeoutSec)";
    }
}
