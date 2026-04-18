#:property TargetFramework=net11.0

const string BaseUrl = "https://cruzlauroiii.github.io/wolfstruckingco.com";

(string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
[
    ("hero-rule", "/css/app.css", "grep", "\\.Hero\\{[^}]{0,80}", "GET", 1),
];
