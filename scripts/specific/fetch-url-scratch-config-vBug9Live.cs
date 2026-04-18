#:property TargetFramework=net11.0

const string BaseUrl = "https://cruzlauroiii.github.io/wolfstruckingco.com";

(string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
[
    ("apply-grep-chat", "/Apply/", "grep", "href=\\\"Chat\\\"", "GET", 1),
    ("apply-grep-applicant", "/Apply/", "grep", "href=\\\"Applicant\\\"", "GET", 1),
    ("chat-head", "/Chat/", "head", "", "GET", 1),
];
