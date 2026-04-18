return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV37
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";
        public const string Find_01 = "    var ScenePathSlug = Route == \"/Chat/\" && SceneChat.ContainsKey(N) ? \"Chat/\" + Pad : Route.Trim('/');\r\n    var ScenePathRoute = Route == \"/Chat/\" && SceneChat.ContainsKey(N) ? \"/Chat/\" + Pad + \"/\" : Route;";
        public const string Replace_01 = "    var IsPerSceneRoute = (Route == \"/Chat/\" && SceneChat.ContainsKey(N)) || Route == \"/Documents/\" || Route == \"/Apply/\" || Route == \"/Map/\" || Route == \"/Track/\" || Route == \"/Dashboard/\" || Route == \"/Marketplace/\" || Route == \"/Schedule/\" || Route == \"/HiringHall/\" || Route == \"/Admin/\" || Route == \"/Investors/KPI/\";\r\n    var ScenePathSlug = IsPerSceneRoute ? Route.Trim('/') + \"/\" + Pad : Route.Trim('/');\r\n    var ScenePathRoute = IsPerSceneRoute ? Route + Pad + \"/\" : Route;";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
