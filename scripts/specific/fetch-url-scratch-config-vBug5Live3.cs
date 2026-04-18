#:property TargetFramework=net11.0

const string BaseUrl = "https://cruzlauroiii.github.io/wolfstruckingco.com";

(string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
[
    ("css-head", "/css/app.css", "head", "", "GET", 1),
    ("hero-any", "/css/app.css", "grep", "Hero", "GET", 1),
];
