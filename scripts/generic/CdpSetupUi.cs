using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using File = System.IO.File;
using Path = System.IO.Path;

namespace CdpTool;

public sealed partial class CdpCli
{
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
}
