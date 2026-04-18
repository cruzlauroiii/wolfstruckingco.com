#:property TargetFramework=net11.0

const string BaseUrl = "https://cruzlauroiii.github.io/wolfstruckingco.com";

(string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
[
    ("root-grep-hero", "/", "grep", "\\.Hero\\{", "GET", 1),
    ("root-grep-padding", "/", "grep", "padding:1rem 18px 2rem", "GET", 1),
];
