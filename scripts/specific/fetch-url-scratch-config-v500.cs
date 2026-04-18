#:property TargetFramework=net11.0

const string BaseUrl = "https://cruzlauroiii.github.io";

(string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
[
    ("dashboard-deploy-marker", "/wolfstruckingco.com/Dashboard/", "grep", "location\\.replace\\('/wolfstruckingco\\.com/'\\)", "GET", 1),
];
