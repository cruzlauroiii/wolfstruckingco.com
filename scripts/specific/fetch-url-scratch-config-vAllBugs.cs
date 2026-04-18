namespace WolfsTruckingCo.Scripts.Specific;

public static class FetchUrlScratchConfigVAllBugs
{
    public const string BaseUrl = "https://cruzlauroiii.github.io";
    public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes = [
        ("AuthWasmGet", "/wolfstruckingco.com/app/_framework/Microsoft.AspNetCore.Components.Authorization.b9dtadca25.wasm", "save", "main/scripts/specific/auth-wasm-bytes.bin", "GET", 1),
    ];
}
