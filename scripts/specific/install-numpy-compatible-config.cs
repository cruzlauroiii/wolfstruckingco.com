return 0;

namespace Scripts
{
    internal static class InstallNumpyCompatibleConfig
    {
        public const string Package = "--force-reinstall numpy==2.3.5";
        public const string TimeoutMs = "600000";
    }
}
