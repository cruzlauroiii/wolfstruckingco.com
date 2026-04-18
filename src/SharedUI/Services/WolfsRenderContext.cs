using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SharedUI.Services;

public static partial class WolfsRenderContext
{
    private const string RootRoute = "/";
    private const string Slash = "/";
    private const string TitleSeparator = " · ";
    private const string HomeTitle = "Home";
    private const string Space = " ";

    public static string CurrentRoute { get; set; } = RootRoute;
    public static int CurrentStep { get; set; }
    public static bool MenuOpen { get; set; }

    public static List<ChatTurn> ChatHistory { get; set; } = [];

    public sealed record ChatTurn(string Role, string Text, string Scan);

    public static string CurrentTitle
    {
        get
        {
            var Path = (CurrentRoute ?? RootRoute).Trim('/');
            if (string.IsNullOrEmpty(Path)) { return HomeTitle; }
            var Parts = Path.Split(Slash).Select(SpaceCamel);
            return string.Join(TitleSeparator, Parts);
        }
    }

    [GeneratedRegex("(?<=[a-z])(?=[A-Z])")]
    private static partial Regex CamelBoundary();

    private static string SpaceCamel(string S) => CamelBoundary().Replace(S, Space);
}
