return 0;

namespace Scripts
{
    internal static class SceneConfig
    {
        public const string Pad = "001";
        public const string Url = "https://cruzlauroiii.github.io/wolfstruckingco.com/?cb=001";
        public const string HydrateSelector = "#app > .TopBar a[href*=Login]";
        public const string BeforeShotJs = "() => { try { ['wolfs_role','wolfs_email','wolfs_session','wolfs_sso'].forEach(function(k){localStorage.removeItem(k);}); } catch(e){} location.reload(); return 'cleared-and-reloaded'; }";
        public const string Narration = "Wolfs moves cars across the world.";
    }
}
