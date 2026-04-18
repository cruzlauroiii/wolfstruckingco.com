return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV70
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\generate-statics.cs";

        public const string Find_01 = "Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));\nvar Provider = Services.BuildServiceProvider();";
        public const string Replace_01 = "Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));\nServices.AddAuthorizationCore();\nServices.AddSingleton<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, LocalStorageAuthStateProvider>();\nvar Provider = Services.BuildServiceProvider();";

        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
