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
