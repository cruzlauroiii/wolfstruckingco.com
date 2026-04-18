return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV302
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ChatBox.razor.cs";
        public const string Find_01 = "using Microsoft.AspNetCore.Components.Web;\nusing SharedUI.Services;";
        public const string Replace_01 = "using Microsoft.AspNetCore.Components.Forms;\nusing Microsoft.AspNetCore.Components.Web;\nusing SharedUI.Services;";
        public const string Find_02 = "    private static Task AttachAsync() => Task.CompletedTask;";
        public const string Replace_02 = "    private const string AttachedNotePrefix = \"Attached: \";\n\n    private Task OnFilesAttachedAsync(InputFileChangeEventArgs E)\n    {\n        var Names = E.GetMultipleFiles().Select(F => F.Name);\n        var Joined = string.Join(\", \", Names);\n        if (string.IsNullOrEmpty(Joined)) { return Task.CompletedTask; }\n        Live.Add(new ChatMessage(RoleUser, AttachedNotePrefix + Joined));\n        StateHasChanged();\n        return Task.CompletedTask;\n    }";
    }
}
