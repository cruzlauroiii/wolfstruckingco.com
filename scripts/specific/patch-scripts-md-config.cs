return 0;

namespace Scripts
{
    internal static class PatchScriptsMdConfig
    {
        public const string Path = @"C:\repo\public\wolfstruckingco.com\main\SCRIPTS.md";
        public const string Anchor = "| `scripts/tail-file.cs` |";
        public const string DuplicateMarker = "scripts/dump-file.cs";
        public const string AddedRows = "| `scripts/dump-file.cs` | `scripts/` | Print full file / line range / byte range / regex-grep — replaces ad-hoc Read on generated artifacts |\n| `scripts/patch-file.cs` | `scripts/` | Substring or regex replace on a file — replaces ad-hoc Edit on non-cs files |\n| `scripts/git-commit-push.cs` | `scripts/` | git add -u + amend (or new) + force-push to main |";
    }
}
