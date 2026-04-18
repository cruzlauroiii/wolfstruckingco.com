public static class Config
{
    public const string TargetFile = "main/src/SharedUI/Components/ChatBox.razor.cs";
    public const string Find_01 = "    private Task OnFilesAttachedAsync(InputFileChangeEventArgs E)\n    {\n        var Names = E.GetMultipleFiles().Select(F => F.Name);\n        var Joined = string.Join(JoinSeparator, Names);\n        if (string.IsNullOrEmpty(Joined)) { return Task.CompletedTask; }\n        Live.Add(new ChatMessage(RoleUser, AttachedNotePrefix + Joined));\n        StateHasChanged();\n        return Task.CompletedTask;\n    }\n\n    ";
    public const string Replace_01 = "    ";
}
