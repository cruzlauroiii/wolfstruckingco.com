using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using File = System.IO.File;
using Path = System.IO.Path;

namespace CdpTool;

public sealed partial class CdpCli
{
private static (string Command, Dictionary<string, object> Args) ParseArgs(string[] Argv)
    {
        var Command = Argv[0];
        var Result = new Dictionary<string, object>(StringComparer.Ordinal);
        var Index = 1;
        while (Index < Argv.Length)
        {
            if (Argv[Index].StartsWith(CdpArg.ArgPrefix, StringComparison.Ordinal))
            {
                var Key = Argv[Index][2..];
                if (Key.StartsWith(CdpArg.NoPrefix, StringComparison.Ordinal))
                {
                    Result[Key[3..]] = false;
                    Index++;
                    continue;
                }

                if (Index + 1 < Argv.Length && !Argv[Index + 1].StartsWith(CdpArg.ArgPrefix, StringComparison.Ordinal))
                {
                    var Value = Argv[Index + 1];
                    Index += 2;
                    if (bool.TryParse(Value, out var BoolValue))
                    {
                        Result[Key] = BoolValue;
                    }
                    else if (int.TryParse(Value, System.Globalization.CultureInfo.InvariantCulture, out var IntValue))
                    {
                        Result[Key] = IntValue;
                    }
                    else if (double.TryParse(Value, System.Globalization.CultureInfo.InvariantCulture, out var DoubleValue))
                    {
                        Result[Key] = DoubleValue;
                    }
                    else
                    {
                        Result[Key] = Value;
                    }
                }
                else
                {
                    Result[Key] = true;
                    Index++;
                }
            }
            else
            {
                switch (Command)
                {
                    case "click" or "hover":
                        if (!Result.ContainsKey(CdpArg.Uid))
                        {
                            Result[CdpArg.Uid] = Argv[Index];
                        }

                        break;
                    case "fill":
                        if (!Result.ContainsKey(CdpArg.Uid))
                        {
                            Result[CdpArg.Uid] = Argv[Index];
                        }
                        else if (!Result.ContainsKey(CdpKey.Value))
                        {
                            Result[CdpKey.Value] = Argv[Index];
                        }

                        break;
                    case "select_page" or "close_page":
                        if (!Result.ContainsKey(CdpArg.PageId))
                        {
                            Result[CdpArg.PageId] = int.Parse(Argv[Index], System.Globalization.CultureInfo.InvariantCulture);
                        }

                        break;
                    case "new_page":
                        if (!Result.ContainsKey(CdpKey.Url))
                        {
                            Result[CdpKey.Url] = Argv[Index];
                        }

                        break;
                    case "evaluate_script":
                        if (!Result.ContainsKey(CdpArg.Function))
                        {
                            Result[CdpArg.Function] = Argv[Index];
                        }

                        break;
                    case "press_key":
                        if (!Result.ContainsKey(CdpKey.Key))
                        {
                            Result[CdpKey.Key] = Argv[Index];
                        }

                        break;
                    case "type_text":
                        if (!Result.ContainsKey(CdpKey.Text))
                        {
                            Result[CdpKey.Text] = Argv[Index];
                        }

                        break;
                    case "resize_page":
                        if (!Result.ContainsKey(CdpKey.Width))
                        {
                            Result[CdpKey.Width] = int.Parse(Argv[Index], System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else if (!Result.ContainsKey(CdpKey.Height))
                        {
                            Result[CdpKey.Height] = int.Parse(Argv[Index], System.Globalization.CultureInfo.InvariantCulture);
                        }

                        break;
                    case "handle_dialog":
                        if (!Result.ContainsKey(CdpArg.Action))
                        {
                            Result[CdpArg.Action] = Argv[Index];
                        }

                        break;
                    case "drag":
                        if (!Result.ContainsKey(CdpArg.FromUid))
                        {
                            Result[CdpArg.FromUid] = Argv[Index];
                        }
                        else if (!Result.ContainsKey(CdpArg.ToUid))
                        {
                            Result[CdpArg.ToUid] = Argv[Index];
                        }

                        break;
                }

                Index++;
            }
        }

        return (Command, Result);
    }

    private static void PrintHelp()
    {
        Console.Write("chrome-devtools.cs - Pure .NET 11 Chrome DevTools CLI\n");
        Console.Write("\n");
        Console.Write("Usage: dotnet run -- <command> [args] [--options]\n");
        Console.Write("\n");
        Console.Write("Connects via raw CDP WebSocket. Auto-clicks Allow prompt (DPI-aware).\n");
        Console.Write("\n");
        Console.Write("Navigation:\n");
        Console.Write("  list_pages                              List open pages\n");
        Console.Write("  select_page <pageId>                    Select page\n");
        Console.Write("  close_page <pageId>                     Close page\n");
        Console.Write("  new_page <url>                          Open new tab\n");
        Console.Write("  navigate_page --type url --url <url>    Navigate (url|back|forward|reload)\n");
        Console.Write("\n");
        Console.Write("Debugging:\n");
        Console.Write("  take_screenshot [--filePath] [--format] [--fullPage] [--quality]\n");
        Console.Write("  take_snapshot [--filePath]\n");
        Console.Write("  evaluate_script <function>              Run JS\n");
        Console.Write("  list_console_messages                   Console output\n");
        Console.Write("  list_network_requests                   Network requests\n");
        Console.Write("\n");
        Console.Write("Input:\n");
        Console.Write("  click <uid> [--dblClick]                Click element\n");
        Console.Write("  hover <uid>                             Hover element\n");
        Console.Write("  fill <uid> <value>                      Fill input/select\n");
        Console.Write("  type_text <text> [--submitKey Enter]    Type text\n");
        Console.Write("  press_key <key>                         Key combo (Control+A)\n");
        Console.Write("  drag <from_uid> <to_uid>               Drag and drop\n");
        Console.Write("  handle_dialog <accept|dismiss>          Handle dialog\n");
        Console.Write("  upload_file --uid <uid> --filePath <p>  Upload file\n");
        Console.Write("\n");
        Console.Write("Emulation:\n");
        Console.Write("  resize_page <width> <height>            Resize viewport\n");
        Console.Write("  emulate [--userAgent] [--geolocation] [--colorScheme]\n");
        Console.Write("\n");
        Console.Write("Utility:\n");
        Console.Write("  allow                                   Click Allow prompt\n");
    }
}
