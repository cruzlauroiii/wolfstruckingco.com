#:property TargetFramework=net11.0

const string BaseUrl = "https://cruzlauroiii.github.io/wolfstruckingco.com";

(string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
[
    ("root-grep-link", "/", "grep", "<link[^>]*\\.css", "GET", 1),
    ("root-grep-style", "/", "grep", "<style", "GET", 1),
];
