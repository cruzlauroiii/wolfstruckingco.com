return 0;

namespace Scripts
{
    internal static class InstallPaddlePaddleCompatibleConfig
    {
        public const string Package = "--force-reinstall paddlepaddle==3.2.2";
        public const string TimeoutMs = "900000";
    }
}
