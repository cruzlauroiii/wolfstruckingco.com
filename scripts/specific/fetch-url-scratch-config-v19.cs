return 0;

#:include SharedSpecifics.cs

namespace Scripts
{
    internal static class FetchUrlScratchConfigV19
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io";
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("apply-grep-chat-link", "/wolfstruckingco.com/Apply/", "grep", "href=\"Chat\"", "GET", 1),
            ("apply-grep-applicant-link", "/wolfstruckingco.com/Apply/", "grep", "href=\"Applicant\"", "GET", 1),
        ];
    }
}
