return 0;

#:include SharedSpecifics.cs

namespace Scripts
{
    internal static class FetchUrlScratchConfigV23
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io";
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("applicant-route", "/wolfstruckingco.com/Applicant/", "head", "", "GET", 1),
            ("applicant-noslash", "/wolfstruckingco.com/Applicant", "head", "", "GET", 1),
            ("chat-route", "/wolfstruckingco.com/Chat/", "head", "", "GET", 1),
        ];
    }
}
