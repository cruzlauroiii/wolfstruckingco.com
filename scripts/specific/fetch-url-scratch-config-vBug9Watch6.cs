#:property TargetFramework=net11.0

const string BaseUrl = "https://cruzlauroiii.github.io/wolfstruckingco.com";

(string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
[
    ("apply-href-chat", "/Apply/", "grep", "href=\"Chat\"", "GET", 1),
    ("apply-href-applicant", "/Apply/", "grep", "href=\"Applicant\"", "GET", 1),
];
