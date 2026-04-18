#:property TargetFramework=net11.0

const string BaseUrl = "https://cruzlauroiii.github.io/wolfstruckingco.com";

(string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
[
    ("chat-tel-anchor", "/Chat/", "grep", "href=\"tel:", "GET", 1),
];
