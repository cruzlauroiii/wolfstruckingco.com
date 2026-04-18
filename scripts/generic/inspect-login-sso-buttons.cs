return 0;

namespace Scripts
{
    internal static class InspectLoginSsoButtonsConfig
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io/wolfstruckingco.com";

        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("Login: SsoBtn occurrences", "/Login/", "grep", "SsoBtn", "GET", 1),
            ("Login: Google text", "/Login/", "grep", "Google", "GET", 1),
            ("Login: any sso= reference", "/Login/", "grep", "sso=", "GET", 1),
            ("Login: any oauth reference", "/Login/", "grep", "oauth", "GET", 1),
        ];
    }
}
