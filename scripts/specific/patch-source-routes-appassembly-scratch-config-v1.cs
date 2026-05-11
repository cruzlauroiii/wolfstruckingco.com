return 0;

namespace Scripts
{
    internal static class PatchSourceRoutesAppassemblyScratchConfigV1
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\Routes.razor";
        public const string Find_01 = "AppAssembly=\"typeof(Routes).Assembly\"";
        public const string Replace_01 = "AppAssembly=\"@typeof(Routes).Assembly\"";
    }
}
