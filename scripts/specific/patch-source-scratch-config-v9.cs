return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV9
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ChatBox.razor.cs";
        public const string Find_01 = "    private const string RoleAssistant = \"assistant\";\n    private const string RoleUser = \"user\";";
        public const string Replace_01 = "    private const string RoleAssistant = \"assistant\";\n    private const string RoleUser = \"user\";\n    private const string RoleAgent = \"agent\";";
        public const string Find_02 = "            var Role = string.Equals(Turn.Role, \"agent\", StringComparison.OrdinalIgnoreCase) ? RoleAssistant : RoleUser;";
        public const string Replace_02 = "            var Role = string.Equals(Turn.Role, RoleAgent, StringComparison.OrdinalIgnoreCase) ? RoleAssistant : RoleUser;";
    }
}
