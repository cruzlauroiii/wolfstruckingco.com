#:property TargetFramework=net11.0

const string BaseUrl = "https://cruzlauroiii.github.io/wolfstruckingco.com";

(string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
[
    ("root-head", "/", "head", "", "GET", 1),
    ("root-grep-css", "/", "grep", "app\\.css", "GET", 1),
];
