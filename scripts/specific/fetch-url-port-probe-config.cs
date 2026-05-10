namespace Scripts
{
    internal static class FetchUrlPortProbeConfig
    {
        public static (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes = [
            ("cdp-serve-9333", "http://127.0.0.1:9333/", "head", "", "GET", 0),
            ("local-http-8080", "http://127.0.0.1:8080/", "head", "", "GET", 0),
        ];
    }
}
