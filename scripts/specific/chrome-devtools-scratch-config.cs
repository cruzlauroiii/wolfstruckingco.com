return 0;

namespace Scripts
{
    internal static class CdpRun
    {
        public const string Command = "evaluate_script";
        public const string PageId = "1";
        public const string Function = "() => fetch('https://cruzlauroiii.github.io/wolfstruckingco.com/Map/?cb=' + Date.now()).then(r => r.text()).then(t => ({ hasMapStageFull: t.includes('MapStageFull'), hasNoActiveNav: t.includes('No active navigation'), hasMapStage: t.includes('MapStage') })).then(o => JSON.stringify(o))";
        public const string OutputPath = @"C:\Users\user1\AppData\Local\Temp\gh-map-check.txt";
    }
}
