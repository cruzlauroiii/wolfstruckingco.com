using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using File = System.IO.File;
using Path = System.IO.Path;

namespace CdpTool;

public sealed partial class CdpCli
{
    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetProcessDPIAware();

    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr Handle);

    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr Handle, int Command);

    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetCursorPos(int X, int Y);

    [LibraryImport("user32.dll", EntryPoint = "mouse_event")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial void MouseEvent(uint Flags, uint X, uint Y, uint Data, int ExtraInfo);

    [LibraryImport("user32.dll", EntryPoint = "keybd_event")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial void KeybdEvent(byte Key, byte Scan, uint Flags, UIntPtr Extra);

    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SwitchToThisWindow(IntPtr Handle, [MarshalAs(UnmanagedType.Bool)] bool AltTab);

    private static void SwitchToWindow(IntPtr Handle)
    {
        SwitchToThisWindow(Handle, true);
        ShowWindow(Handle, CdpWin32.SwMaximize);
        SetForegroundWindow(Handle);
    }

    private static void ClickAllowPrompt(bool Debug = false)
    {
        try
        {
            SetProcessDPIAware();
            var Root = AutomationElement.RootElement;
            var ChromeCondition = new PropertyCondition(AutomationElement.ClassNameProperty, CdpProto.ChromeWidgetClass);
            if (Root.FindFirst(TreeScope.Children, ChromeCondition) == null)
            {
                if (Debug)
                {
                    Console.Error.WriteLine("Chrome not on current desktop, switching right...");
                }

                SwitchDesktopRight();
                if (Root.FindFirst(TreeScope.Children, ChromeCondition) == null)
                {
                    SwitchDesktopLeft();
                    SwitchDesktopLeft();
                }
            }

            foreach (AutomationElement Window in Root.FindAll(TreeScope.Children, ChromeCondition))
            {
                var WindowHandle = new IntPtr(Window.Current.NativeWindowHandle);
                AutomationElement? AllowButton = null;
                var ButtonCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
                if (Debug)
                {
                    Console.Error.WriteLine($"Window: {Window.Current.Name} hwnd={WindowHandle}");
                    foreach (AutomationElement Button in Window.FindAll(TreeScope.Descendants, ButtonCondition))
                    {
                        Console.Error.WriteLine($"  Button: '{Button.Current.Name}' rect={Button.Current.BoundingRectangle}");
                    }
                }

                foreach (AutomationElement Button in Window.FindAll(TreeScope.Descendants, ButtonCondition))
                {
                    if (Button.Current.Name == CdpProto.AllowButtonName)
                    {
                        AllowButton = Button;
                        break;
                    }
                }

                if (AllowButton == null)
                {
                    continue;
                }

                SwitchToWindow(WindowHandle);
                Thread.Sleep(CdpTimeout.ForegroundDelayMs);

                var Rect = AllowButton.Current.BoundingRectangle;
                if (Debug)
                {
                    Console.Error.WriteLine($"Allow button rect: {Rect} empty={Rect.IsEmpty}");
                }

                if (!Rect.IsEmpty)
                {
                    var X = (int)(Rect.X + (Rect.Width / 2));
                    var Y = (int)(Rect.Y + (Rect.Height / 2));
                    if (Debug)
                    {
                        Console.Error.WriteLine($"Mouse click at {X},{Y}");
                    }

                    SetCursorPos(X, Y);
                    Thread.Sleep(50);
                    MouseEvent(CdpWin32.MouseLeftDown, 0, 0, 0, 0);
                    Thread.Sleep(50);
                    MouseEvent(CdpWin32.MouseLeftUp, 0, 0, 0, 0);
                }
                else
                {
                    try
                    {
                        AllowButton.SetFocus();
                    }
                    catch (InvalidOperationException)
                    {
                    }

                    KeybdEvent(0x09, 0, 0, UIntPtr.Zero);
                    KeybdEvent(0x09, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
                    KeybdEvent(0x09, 0, 0, UIntPtr.Zero);
                    KeybdEvent(0x09, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
                    KeybdEvent(0x0D, 0, 0, UIntPtr.Zero);
                    KeybdEvent(0x0D, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
                }

                Console.Error.WriteLine($"{CdpShell.ClickedPrefix} {CdpProto.AllowButtonName}");
                return;
            }

            if (Debug)
            {
                Console.Error.WriteLine("Allow button not found in any Chrome window");
            }

            DismissInfobar(Debug);
        }
        catch (Exception Ex)
        {
            if (Debug)
            {
                Console.Error.WriteLine($"ClickAllowPrompt error: {Ex.Message}");
            }
        }
    }

    private static bool DismissInfobar(bool Debug = false)
    {
        try
        {
            var Root = AutomationElement.RootElement;
            var ChromeCondition = new PropertyCondition(AutomationElement.ClassNameProperty, CdpProto.ChromeWidgetClass);
            foreach (AutomationElement Window in Root.FindAll(TreeScope.Children, ChromeCondition))
            {
                var ButtonCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
                double InfobarY = -1;
                AutomationElement? CloseBtn = null;
                foreach (AutomationElement Button in Window.FindAll(TreeScope.Descendants, ButtonCondition))
                {
                    var Name = Button.Current.Name;
                    var Rect = Button.Current.BoundingRectangle;
                    if (Rect.IsEmpty)
                    {
                        continue;
                    }

                    if (Name == "Turn off in settings")
                    {
                        InfobarY = Rect.Y;
                    }
                }

                if (InfobarY < 0)
                {
                    continue;
                }

                foreach (AutomationElement Button in Window.FindAll(TreeScope.Descendants, ButtonCondition))
                {
                    var Name = Button.Current.Name;
                    var Rect = Button.Current.BoundingRectangle;
                    if (Rect.IsEmpty)
                    {
                        continue;
                    }

                    if (Name == "Close" && Math.Abs(Rect.Y - InfobarY) < 20)
                    {
                        CloseBtn = Button;
                        if (Debug)
                        {
                            Console.Error.WriteLine($"Infobar Close at {(int)Rect.X},{(int)Rect.Y} rect={Rect}");
                        }
                    }
                }

                if (CloseBtn != null)
                {
                    try
                    {
                        if (CloseBtn.TryGetCurrentPattern(InvokePattern.Pattern, out var Pattern))
                        {
                            ((InvokePattern)Pattern).Invoke();
                            Console.Error.WriteLine("Dismissed infobar (invoke)");
                            return true;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                    }

                    var R = CloseBtn.Current.BoundingRectangle;
                    var X = (int)(R.X + (R.Width / 2));
                    var Y = (int)(R.Y + (R.Height / 2));
                    if (Debug)
                    {
                        Console.Error.WriteLine($"Mouse click at {X},{Y}");
                    }

                    SetCursorPos(X, Y);
                    Thread.Sleep(50);
                    MouseEvent(CdpWin32.MouseLeftDown, 0, 0, 0, 0);
                    Thread.Sleep(50);
                    MouseEvent(CdpWin32.MouseLeftUp, 0, 0, 0, 0);
                    Console.Error.WriteLine("Dismissed infobar (click)");
                    return true;
                }
            }

            if (Debug)
            {
                Console.Error.WriteLine("Infobar not found");
            }

            return false;
        }
        catch (Exception Ex)
        {
            if (Debug)
            {
                Console.Error.WriteLine($"DismissInfobar error: {Ex.Message}");
            }

            return false;
        }
    }

    private static void ExecuteScreenshotDesktop(Dictionary<string, object> Args)
    {
        DismissInfobar();
        var Bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
        using var Bitmap = new System.Drawing.Bitmap(Bounds.Width, Bounds.Height);
        using var Graphics = System.Drawing.Graphics.FromImage(Bitmap);
        Graphics.CopyFromScreen(Bounds.Location, System.Drawing.Point.Empty, Bounds.Size);
        var OutputPath = Args.TryGetValue(CdpArg.FilePath, out var FilePath) ? FilePath.ToString()! : Path.Combine(Path.GetTempPath(), string.Concat(CdpProto.ScreenshotPrefix, CdpProto.DesktopScreenshotFile));
        Bitmap.Save(OutputPath);
        Console.WriteLine(string.Concat(CdpMsg.ScreenshotSaved, OutputPath));
    }

    private static void SwitchDesktopRight()
    {
        KeybdEvent(CdpWin32.VkLWin, 0, 0, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkControl, 0, 0, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkRight, 0, 0, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkRight, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkControl, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkLWin, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        Thread.Sleep(CdpTimeout.ForegroundDelayMs);
    }

    private static void SwitchDesktopLeft()
    {
        KeybdEvent(CdpWin32.VkLWin, 0, 0, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkControl, 0, 0, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkLeft, 0, 0, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkLeft, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkControl, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkLWin, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        Thread.Sleep(CdpTimeout.ForegroundDelayMs);
    }

    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint SendInput(uint Count, Input[] Inputs, int Size);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public Keybdinput Ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Keybdinput
    {
        public ushort Vk;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public IntPtr Extra;
    }

    private static void TypeString(string Text)
    {
        foreach (var Ch in Text)
        {
            var Inputs = new Input[2];
            Inputs[0] = new Input { Type = 1, U = new InputUnion { Ki = new Keybdinput { Vk = 0, Scan = Ch, Flags = 4 } } };
            Inputs[1] = new Input { Type = 1, U = new InputUnion { Ki = new Keybdinput { Vk = 0, Scan = Ch, Flags = 4 | 2 } } };
            SendInput(2, Inputs, Marshal.SizeOf<Input>());
        }
    }

    private static void NavigateAddressBar(string Url)
    {
        var Root = AutomationElement.RootElement;
        var ChromeCondition = new PropertyCondition(AutomationElement.ClassNameProperty, CdpProto.ChromeWidgetClass);
        var Window = Root.FindFirst(TreeScope.Children, ChromeCondition);
        if (Window == null)
        {
            Console.Error.WriteLine("Chrome not found");
            return;
        }

        SwitchToWindow(new IntPtr(Window.Current.NativeWindowHandle));
        Thread.Sleep(200);
        var Thread2 = new Thread(() => System.Windows.Forms.Clipboard.SetText(Url));
        Thread2.SetApartmentState(ApartmentState.STA);
        Thread2.Start();
        Thread2.Join();
        KeybdEvent(CdpWin32.VkEscape, 0, 0, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkEscape, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        Thread.Sleep(100);
        KeybdEvent(CdpWin32.VkControl, 0, 0, UIntPtr.Zero);
        KeybdEvent(0x4C, 0, 0, UIntPtr.Zero);
        KeybdEvent(0x4C, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkControl, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        Thread.Sleep(300);
        KeybdEvent(CdpWin32.VkControl, 0, 0, UIntPtr.Zero);
        KeybdEvent(0x56, 0, 0, UIntPtr.Zero);
        KeybdEvent(0x56, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkControl, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        Thread.Sleep(200);
        KeybdEvent(CdpWin32.VkReturn, 0, 0, UIntPtr.Zero);
        KeybdEvent(CdpWin32.VkReturn, 0, CdpWin32.KeyEventUp, UIntPtr.Zero);
        Console.WriteLine($"Navigated to {Url}");
    }

    private static void FocusChrome()
    {
        var Root = AutomationElement.RootElement;
        var ChromeCondition = new PropertyCondition(AutomationElement.ClassNameProperty, CdpProto.ChromeWidgetClass);
        var Window = Root.FindFirst(TreeScope.Children, ChromeCondition);
        if (Window != null)
        {
            SwitchToWindow(new IntPtr(Window.Current.NativeWindowHandle));
            Thread.Sleep(300);
            DismissInfobar();
            Console.WriteLine("Chrome focused");
            return;
        }

        SwitchDesktopRight();
        Window = AutomationElement.RootElement.FindFirst(TreeScope.Children, ChromeCondition);
        if (Window != null)
        {
            SwitchToWindow(new IntPtr(Window.Current.NativeWindowHandle));
            Thread.Sleep(300);
            DismissInfobar();
            Console.WriteLine("Chrome focused");
            return;
        }

        SwitchDesktopLeft();
        SwitchDesktopLeft();
        Window = AutomationElement.RootElement.FindFirst(TreeScope.Children, ChromeCondition);
        if (Window != null)
        {
            SwitchToWindow(new IntPtr(Window.Current.NativeWindowHandle));
            Console.WriteLine("Chrome focused");
        }
        else
        {
            Console.Error.WriteLine("Chrome not found");
        }
    }

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
