return 0;

namespace Scripts
{
    internal static class KillProcsGitConfig
    {
        public static readonly string[] ProcessNames = new[] { "git" };
        public const int OnlyOlderThanSec = 0;
    }
}
